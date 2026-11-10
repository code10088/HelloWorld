using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NetMsgGen;

public static class ProtoParser
{
    public static List<ProtoFile> ParseDirectory(string dir)
    {
        return Directory.GetFiles(dir, "*.proto")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(Parse)
            .ToList();
    }

    public static ProtoFile Parse(string path)
    {
        var text = File.ReadAllText(path);
        var file = new ProtoFile
        {
            Path = path,
            Name = Path.GetFileNameWithoutExtension(path),
            IdBase = MatchInt(text, @"//\s*@id-base\s+(\d+)"),
        };
        var token = Tokenize(text);
        var explicitIds = ParseExplicitIds(text);
        int p = 0;
        while (p < token.Count)
        {
            switch (token[p].Text)
            {
                case "syntax":
                case "import":
                    p = SkipToSemicolon(token, p);
                    break;
                case "package":
                    file.Package = token[p + 1].Text;
                    p = SkipToSemicolon(token, p);
                    break;
                case "option":
                    if (token[p + 1].Text == "csharp_namespace")
                        file.Namespace = token[p + 3].Text;
                    p = SkipToSemicolon(token, p);
                    break;
                case "message":
                    p = ParseMessage(token, p, file, explicitIds);
                    break;
                case "enum":
                    p = ParseEnum(token, p, file);
                    break;
                default:
                    p++;
                    break;
            }
        }
        return file;
    }

    private static int ParseMessage(List<Token> t, int p, ProtoFile file, Dictionary<string, int> explicitIds)
    {
        var msg = new MessageDecl { Name = t[p + 1].Text };
        if (explicitIds.TryGetValue(msg.Name, out var id))
            msg.ExplicitId = id;

        p += 2;
        while (t[p].Text != "{") p++;
        p++;
        while (t[p].Text != "}")
        {
            var word = t[p].Text;
            if (word == "message" || word == "enum" || word == "oneof" || word == "service")
            {
                p = SkipBlock(t, p);
                continue;
            }
            if (word == "option" || word == "reserved" || word == "extensions")
            {
                p = SkipToSemicolon(t, p);
                continue;
            }

            bool repeated = false;
            if (word == "repeated" || word == "optional" || word == "required")
            {
                repeated = word == "repeated";
                p++;
            }
            msg.Fields.Add(new FieldDecl { Type = t[p].Text, Name = t[p + 1].Text, Repeated = repeated });
            p += 2;
            p = SkipToSemicolon(t, p);
        }
        p++;
        file.Messages.Add(msg);
        return p;
    }

    private static int ParseEnum(List<Token> t, int p, ProtoFile file)
    {
        var decl = new EnumDecl { Name = t[p + 1].Text };
        p += 2;
        while (t[p].Text != "{") p++;
        p++;
        while (t[p].Text != "}")
        {
            if (t[p].Text == "option" || t[p].Text == "reserved")
            {
                p = SkipToSemicolon(t, p);
                continue;
            }
            var name = t[p].Text;
            p++;
            if (t[p].Text == "=") p++;
            var value = int.Parse(t[p].Text);
            p = SkipToSemicolon(t, p);
            decl.Values.Add((name, value));
        }
        p++;
        file.Enums.Add(decl);
        return p;
    }

    private static int SkipToSemicolon(List<Token> t, int p)
    {
        while (t[p].Text != ";") p++;
        return p + 1;
    }

    private static int SkipBlock(List<Token> t, int p)
    {
        while (t[p].Text != "{") p++;
        int depth = 0;
        do
        {
            if (t[p].Text == "{") depth++;
            else if (t[p].Text == "}") depth--;
            p++;
        } while (depth > 0);
        return p;
    }

    private static Dictionary<string, int> ParseExplicitIds(string text)
    {
        var map = new Dictionary<string, int>();
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Regex.Match(lines[i], @"^\s*//\s*@id\s+(\d+)\s*$");
            if (!m.Success) continue;
            for (int j = i + 1; j < lines.Length; j++)
            {
                var n = Regex.Match(lines[j], @"^\s*message\s+(\w+)");
                if (n.Success)
                {
                    map[n.Groups[1].Value] = int.Parse(m.Groups[1].Value);
                    break;
                }
                if (lines[j].Trim().Length != 0) break;
            }
        }
        return map;
    }

    private static int? MatchInt(string text, string pattern)
    {
        var m = Regex.Match(text, pattern);
        return m.Success ? int.Parse(m.Groups[1].Value) : null;
    }

    private sealed class Token
    {
        public string Text;
    }

    private static List<Token> Tokenize(string s)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
            }
            else if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/')) i++;
                i += 2;
            }
            else if (c == '"')
            {
                i++;
                var sb = new StringBuilder();
                while (s[i] != '"')
                {
                    if (s[i] == '\\') i++;
                    sb.Append(s[i]);
                    i++;
                }
                i++;
                tokens.Add(new Token { Text = sb.ToString() });
            }
            else if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '.')) i++;
                tokens.Add(new Token { Text = s.Substring(start, i - start) });
            }
            else if (char.IsDigit(c) || (c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                int start = i;
                i++;
                while (i < s.Length && char.IsDigit(s[i])) i++;
                tokens.Add(new Token { Text = s.Substring(start, i - start) });
            }
            else
            {
                tokens.Add(new Token { Text = c.ToString() });
                i++;
            }
        }
        return tokens;
    }
}