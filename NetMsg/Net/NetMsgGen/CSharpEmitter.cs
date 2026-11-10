using System;
using System.Collections.Generic;
using System.Linq;

namespace NetMsgGen;

public static class CSharpEmitter
{
    private static readonly Dictionary<string, (string Cs, string Write, string Read)> Scalars = new()
    {
        ["double"] = ("double", "WriteDouble", "ReadDouble"),
        ["float"] = ("float", "WriteFloat", "ReadFloat"),
        ["int32"] = ("int", "WriteInt", "ReadInt"),
        ["sint32"] = ("int", "WriteInt", "ReadInt"),
        ["sfixed32"] = ("int", "WriteInt", "ReadInt"),
        ["uint32"] = ("uint", "WriteUInt", "ReadUInt"),
        ["fixed32"] = ("uint", "WriteUInt", "ReadUInt"),
        ["int64"] = ("long", "WriteLong", "ReadLong"),
        ["sint64"] = ("long", "WriteLong", "ReadLong"),
        ["sfixed64"] = ("long", "WriteLong", "ReadLong"),
        ["string"] = ("string", "WriteString", "ReadString"),
        ["bytes"] = ("byte[]", "WriteByteArray", "ReadByteArray"),
    };

    public static string Emit(ProtoFile file, Model model)
    {
        var w = new CodeWriter();

        bool usesList = file.Messages.Any(m => m.Fields.Any(f => f.Repeated && !Scalars.ContainsKey(f.Type)));
        if (usesList)
        {
            w.Line("using System.Collections.Generic;");
            w.Blank();
        }

        w.Line("namespace " + model.NamespaceOf(file));
        w.Open();

        bool first = true;
        foreach (var e in file.Enums)
        {
            if (!first) w.Blank();
            first = false;
            EmitEnum(w, e);
        }
        foreach (var m in file.Messages.Where(m => !Model.IsProtocol(m.Name)))
        {
            if (!first) w.Blank();
            first = false;
            EmitMessage(w, m, model);
        }
        foreach (var m in file.Messages.Where(m => Model.IsProtocol(m.Name)))
        {
            if (!first) w.Blank();
            first = false;
            EmitMessage(w, m, model);
        }

        w.Close();
        return w.ToString();
    }

    private static void EmitEnum(CodeWriter w, EnumDecl e)
    {
        w.Line($"public enum {e.Name}");
        w.Open();
        foreach (var (name, value) in e.Values)
            w.Line($"{Pascal(name)} = {value},");
        w.Close();
    }

    private static void EmitMessage(CodeWriter w, MessageDecl m, Model model)
    {
        bool cs = m.Name.StartsWith("CS");
        bool sc = m.Name.StartsWith("SC");
        string interfaces = cs ? "ISerialize" : sc ? "IDeserialize" : "ISerialize, IDeserialize";

        w.Line($"public class {m.Name} : {interfaces}");
        w.Open();

        if (m.Fields.Count > 0)
        {
            foreach (var f in m.Fields)
                w.Line($"public {FieldType(f)} {Name(f)}{Initializer(f)};");
            w.Blank();
        }

        if (!sc) EmitSerialize(w, m, model);
        if (!sc && !cs) w.Blank();
        if (!cs) EmitDeserialize(w, m, model);

        w.Close();
    }

    private static void EmitSerialize(CodeWriter w, MessageDecl m, Model model)
    {
        w.Line("public void Serialize(UnsafeByteBuffer buffer)");
        w.Open();
        foreach (var f in m.Fields)
        {
            var n = Name(f);
            if (Scalars.TryGetValue(f.Type, out var s))
                w.Line(f.Repeated ? $"buffer.{s.Write}Array({n});" : $"buffer.{s.Write}({n});");
            else if (!f.Repeated && model.EnumNames.Contains(f.Type))
                w.Line($"buffer.WriteInt((int){n});");
            else if (f.Repeated)
            {
                w.Line($"buffer.WriteInt({n}.Count);");
                w.Line($"for (int i = 0; i < {n}.Count; i++)");
                w.Open();
                w.Line($"{n}[i].Serialize(buffer);");
                w.Close();
            }
            else
                w.Line($"{n}.Serialize(buffer);");
        }
        w.Close();
    }

    private static void EmitDeserialize(CodeWriter w, MessageDecl m, Model model)
    {
        w.Line("public void Deserialize(UnsafeByteBuffer buffer)");
        w.Open();
        foreach (var f in m.Fields)
        {
            var n = Name(f);
            if (Scalars.TryGetValue(f.Type, out var s))
                w.Line(f.Repeated ? $"{n} = buffer.{s.Read}Array();" : $"{n} = buffer.{s.Read}();");
            else if (!f.Repeated && model.EnumNames.Contains(f.Type))
                w.Line($"{n} = ({f.Type})buffer.ReadInt();");
            else if (f.Repeated)
            {
                var count = m.Fields.Count == 1 ? "count" : Singular(n) + "Count";
                var item = Singular(n);
                w.Line($"int {count} = buffer.ReadInt();");
                if (Model.IsProtocol(m.Name)) w.Line($"{n}.Clear();");
                w.Line($"for (int i = 0; i < {count}; i++)");
                w.Open();
                w.Line($"var {item} = new {f.Type}();");
                w.Line($"{item}.Deserialize(buffer);");
                w.Line($"{n}.Add({item});");
                w.Close();
            }
            else
            {
                w.Line($"{n} = new {f.Type}();");
                w.Line($"{n}.Deserialize(buffer);");
            }
        }
        w.Close();
    }

    private static string FieldType(FieldDecl f)
    {
        if (Scalars.TryGetValue(f.Type, out var s)) return f.Repeated ? s.Cs + "[]" : s.Cs;
        return f.Repeated ? $"List<{f.Type}>" : f.Type;
    }

    private static string Initializer(FieldDecl f)
        => f.Repeated && !Scalars.ContainsKey(f.Type) ? $" = new List<{f.Type}>()" : "";

    private static string Name(FieldDecl f)
        => f.Repeated && !f.Name.EndsWith('s') ? f.Name + "s" : f.Name;

    private static string Singular(string name)
        => name.EndsWith('s') ? name[..^1] : name;

    private static string Pascal(string name)
        => string.Concat(name.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
}