using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetMsgGen;

public class ProtoFile
{
    public string Path;
    public string Name;
    public string Namespace;
    public string Package;
    public int? IdBase;
    public readonly List<MessageDecl> Messages = new();
    public readonly List<EnumDecl> Enums = new();
}

public class MessageDecl
{
    public string Name;
    public int? ExplicitId;
    public readonly List<FieldDecl> Fields = new();
}

public class FieldDecl
{
    public string Type;
    public string Name;
    public bool Repeated;
    public int Number;
}

public class EnumDecl
{
    public string Name;
    public readonly List<(string Name, int Value)> Values = new();
}

public class Model
{
    public readonly HashSet<string> EnumNames = new();
    public readonly HashSet<string> MessageNames = new();

    public Model(List<ProtoFile> files)
    {
        foreach (var file in files)
        {
            foreach (var e in file.Enums) EnumNames.Add(e.Name);
            foreach (var m in file.Messages) MessageNames.Add(m.Name);
        }
    }

    public string NamespaceOf(ProtoFile file) => file.Namespace ?? file.Package ?? "Message";

    public static bool IsProtocol(string name) => name.StartsWith("CS") || name.StartsWith("SC");
}

public static class ProtoParser
{
    private static readonly Regex IdBasePattern = new(@"//\s*@id-base\s+(\d+)", RegexOptions.Compiled);
    private static readonly Regex IdLinePattern = new(@"^\s*//\s*@id\s+(\d+)\s*$", RegexOptions.Compiled);
    private static readonly Regex MessageLinePattern = new(@"^\s*message\s+(\w+)", RegexOptions.Compiled);

    public static List<ProtoFile> ParseDirectory(string dir)
    {
        return Directory.GetFiles(dir, "*.proto")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(Parse)
            .ToList();
    }

    public static ProtoFile Parse(string path)
    {
        return new Parser(path).Run();
    }

    private class Token
    {
        public string Text;
        public int Line;
    }

    private class Parser
    {
        private readonly ProtoFile file;
        private readonly List<Token> token;
        private readonly Dictionary<string, int> ids;
        private int pos;

        public Parser(string path)
        {
            file = new ProtoFile
            {
                Path = path,
                Name = Path.GetFileNameWithoutExtension(path),
            };
            var text = File.ReadAllText(path);
            var match = IdBasePattern.Match(text);
            if (match.Success) file.IdBase = int.Parse(match.Groups[1].Value);
            ids = ParseExplicitIds(text);
            token = Tokenize(file.Name + ".proto", text);
        }

        public ProtoFile Run()
        {
            while (pos < token.Count)
            {
                switch (Peek().Text)
                {
                    case ";":
                        pos++;
                        break;
                    case "syntax":
                    case "import":
                        SkipToSemicolon();
                        break;
                    case "package":
                        pos++;
                        file.Package = Next().Text;
                        SkipToSemicolon();
                        break;
                    case "option":
                        ParseOption();
                        break;
                    case "message":
                        ParseMessage();
                        break;
                    case "enum":
                        ParseEnum();
                        break;
                    default:
                        throw Error("unsupported token '" + Peek().Text + "'");
                }
            }
            return file;
        }

        private void ParseOption()
        {
            pos++;
            var name = Next().Text;
            while (pos < token.Count && token[pos].Text != "=") pos++;
            if (pos >= token.Count) throw Error("missing '=' in option");
            pos++;
            var value = Next().Text;
            if (name == "csharp_namespace" && file.Namespace == null) file.Namespace = value;
            SkipToSemicolon();
        }

        private void ParseMessage()
        {
            pos++;
            var message = new MessageDecl { Name = Next().Text };
            if (ids.TryGetValue(message.Name, out var id)) message.ExplicitId = id;
            Expect("{");

            var names = new HashSet<string>(StringComparer.Ordinal);
            var last = 0;
            while (true)
            {
                var head = Peek().Text;
                if (head == "}")
                {
                    pos++;
                    break;
                }
                if (head == "oneof") throw Error("oneof is not supported");
                if (head == "message" || head == "enum") throw Error("nested type is not supported");
                if (head == "option" || head == "reserved" || head == "extensions")
                {
                    SkipToSemicolon();
                    continue;
                }
                if (head == ";")
                {
                    pos++;
                    continue;
                }

                var repeated = false;
                if (head == "repeated" || head == "optional" || head == "required")
                {
                    repeated = head == "repeated";
                    pos++;
                }

                var type = Next().Text;
                if (type == ".") throw Error("fully qualified field type is not supported");
                if (type == "map") throw Error("map is not supported");

                var name = Next().Text;
                Expect("=");
                var number = ParseInt(Next(), "field number");
                if (number <= last)
                    throw Error($"field '{name}' number {number} must be greater than {last}, fields are written in declaration order");
                last = number;

                if (Peek().Text == "[") SkipTo("]");
                Expect(";");

                if (!names.Add(name)) throw Error("duplicate field '" + name + "'");
                message.Fields.Add(new FieldDecl { Type = type, Name = name, Repeated = repeated, Number = number });
            }

            file.Messages.Add(message);
        }

        private void ParseEnum()
        {
            pos++;
            var decl = new EnumDecl { Name = Next().Text };
            Expect("{");

            var names = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var head = Peek().Text;
                if (head == "}")
                {
                    pos++;
                    break;
                }
                if (head == "option" || head == "reserved")
                {
                    SkipToSemicolon();
                    continue;
                }
                if (head == ";")
                {
                    pos++;
                    continue;
                }

                var name = Next().Text;
                Expect("=");
                var value = ParseInt(Next(), "enum value");
                if (Peek().Text == "[") SkipTo("]");
                Expect(";");

                if (!names.Add(name)) throw Error("duplicate enum value '" + name + "'");
                decl.Values.Add((name, value));
            }

            file.Enums.Add(decl);
        }

        private int ParseInt(Token value, string what)
        {
            if (!int.TryParse(value.Text, out var result)) throw Error("invalid " + what + " '" + value.Text + "'");
            return result;
        }

        private Token Peek()
        {
            if (pos >= token.Count) throw Error("unexpected end of file");
            return token[pos];
        }

        private Token Next()
        {
            var value = Peek();
            pos++;
            return value;
        }

        private void Expect(string text)
        {
            if (Next().Text != text) throw Error("expected '" + text + "'");
        }

        private void SkipTo(string text)
        {
            while (pos < token.Count && token[pos].Text != text) pos++;
            if (pos >= token.Count) throw Error("missing '" + text + "'");
            pos++;
        }

        private void SkipToSemicolon() => SkipTo(";");

        private Exception Error(string message)
        {
            var line = token.Count == 0 ? 0 : token[Math.Min(pos, token.Count - 1)].Line;
            return new Exception($"{file.Name}.proto:{line}: {message}");
        }
    }

    private static Dictionary<string, int> ParseExplicitIds(string text)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var match = IdLinePattern.Match(lines[i]);
            if (!match.Success) continue;
            for (int j = i + 1; j < lines.Length; j++)
            {
                var name = MessageLinePattern.Match(lines[j]);
                if (name.Success)
                {
                    map[name.Groups[1].Value] = int.Parse(match.Groups[1].Value);
                    break;
                }
                if (lines[j].Trim().Length != 0) break;
            }
        }
        return map;
    }

    private static List<Token> Tokenize(string name, string text)
    {
        var list = new List<Token>();
        var line = 1;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\n')
            {
                line++;
                i++;
                continue;
            }
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '/')
            {
                while (i < text.Length && text[i] != '\n') i++;
                continue;
            }
            if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var start = line;
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                {
                    if (text[i] == '\n') line++;
                    i++;
                }
                if (i + 1 >= text.Length) throw new Exception($"{name}:{start}: unterminated block comment");
                i += 2;
                continue;
            }
            if (c == '"')
            {
                var start = line;
                i++;
                var builder = new StringBuilder();
                while (true)
                {
                    if (i >= text.Length) throw new Exception($"{name}:{start}: unterminated string");
                    if (text[i] == '"')
                    {
                        i++;
                        break;
                    }
                    if (text[i] == '\\' && i + 1 < text.Length) i++;
                    builder.Append(text[i]);
                    i++;
                }
                list.Add(new Token { Text = builder.ToString(), Line = start });
                continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || text[i] == '.')) i++;
                list.Add(new Token { Text = text.Substring(start, i - start), Line = line });
                continue;
            }
            if (char.IsDigit(c) || (c == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
            {
                var start = i;
                i++;
                while (i < text.Length && char.IsDigit(text[i])) i++;
                list.Add(new Token { Text = text.Substring(start, i - start), Line = line });
                continue;
            }
            list.Add(new Token { Text = c.ToString(), Line = line });
            i++;
        }
        return list;
    }
}