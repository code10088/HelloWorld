using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public static class ProtoParser
{
    // 匹配 message 块
    private static readonly Regex MsgRegex = new Regex(@"message\s+(\w+)\s*\{([^}]*)\}", RegexOptions.Singleline);
    // 匹配字段：类型 名称 = 编号;
    private static readonly Regex FieldRegex = new Regex(@"^\s*(?:repeated\s+)?(\w+)\s+(\w+)\s*=\s*\d+\s*;", RegexOptions.Multiline);
    // 匹配枚举块
    private static readonly Regex EnumRegex = new Regex(@"enum\s+(\w+)\s*\{([^}]*)\}", RegexOptions.Singleline);
    // 匹配枚举值
    private static readonly Regex EnumValueRegex = new Regex(@"^\s*(\w+)\s*=\s*(\d+)\s*;", RegexOptions.Multiline);

    private static readonly Dictionary<string, string> ProtoTypeMap = new Dictionary<string, string>
    {
        { "uint32", "uint" },
        { "int32",  "int" },
        { "uint64", "ulong" },
        { "int64",  "long" },
        { "float",  "float" },
        { "double", "double" },
        { "bool",   "bool" },
        { "string", "string" },
        { "bytes",  "byte[]" },
    };

    public static string GenerateMsgClass(string protoFile)
    {
        string content = File.ReadAllText(protoFile);
        string namespaceName = "Message";
        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // 生成枚举
        foreach (Match em in EnumRegex.Matches(content))
        {
            string enumName = em.Groups[1].Value;
            string enumBody = em.Groups[2].Value;
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");
            foreach (Match ev in EnumValueRegex.Matches(enumBody))
            {
                string valName = ToCamelCase(ev.Groups[1].Value);
                string valNum = ev.Groups[2].Value;
                sb.AppendLine($"        {valName} = {valNum},");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // 收集所有消息名，用于判断嵌套消息类型
        var allMsgNames = new HashSet<string>();
        foreach (Match mm in MsgRegex.Matches(content))
            allMsgNames.Add(mm.Groups[1].Value);

        // 生成消息类
        foreach (Match mm in MsgRegex.Matches(content))
        {
            string msgName = mm.Groups[1].Value;
            string msgBody = mm.Groups[2].Value;
            bool isCS = msgName.StartsWith("CS");
            bool isSC = msgName.StartsWith("SC");
            bool isOther = !isCS && !isSC;

            string iface = isCS ? "ISerialize" : isSC ? "IDeserialize" : "ISerialize, IDeserialize";
            sb.AppendLine($"    public class {msgName} : {iface}");
            sb.AppendLine("    {");

            // 解析字段
            var fields = new List<(string csType, string name, bool repeated)>();
            foreach (Match fm in FieldRegex.Matches(msgBody))
            {
                bool repeated = fm.Value.TrimStart().StartsWith("repeated");
                string protoType = fm.Groups[1].Value;
                string fieldName = fm.Groups[2].Value;
                string csType = ProtoTypeMap.TryGetValue(protoType, out string mapped) ? mapped : protoType;
                fields.Add((csType, fieldName, repeated));
            }

            // 字段声明
            foreach (var (csType, name, repeated) in fields)
            {
                if (repeated)
                    sb.AppendLine($"        public List<{csType}> {name} = new List<{csType}>();");
                else
                    sb.AppendLine($"        public {csType} {name};");
            }

            if (fields.Count > 0) sb.AppendLine();

            // Serialize
            if (isCS || isOther)
            {
                sb.AppendLine("        public void Serialize(UnsafeByteBuffer buffer)");
                sb.AppendLine("        {");
                foreach (var (csType, name, repeated) in fields)
                {
                    if (repeated)
                    {
                        sb.AppendLine($"            buffer.WriteInt({name}.Count);");
                        sb.AppendLine($"            for (int i = 0; i < {name}.Count; i++)");
                        if (allMsgNames.Contains(csType))
                            sb.AppendLine($"                {name}[i].Serialize(buffer);");
                        else
                            sb.AppendLine($"                buffer.{GetWriteMethod(csType)}({name}[i]);");
                    }
                    else
                    {
                        if (allMsgNames.Contains(csType))
                            sb.AppendLine($"            {name}.Serialize(buffer);");
                        else
                            sb.AppendLine($"            buffer.{GetWriteMethod(csType)}({name});");
                    }
                }
                sb.AppendLine("        }");
            }

            // Deserialize
            if (isSC || isOther)
            {
                if (isCS || isOther) sb.AppendLine();
                sb.AppendLine("        public void Deserialize(UnsafeByteBuffer buffer)");
                sb.AppendLine("        {");
                foreach (var (csType, name, repeated) in fields)
                {
                    if (repeated)
                    {
                        sb.AppendLine($"            int {name}Count = buffer.ReadInt();");
                        sb.AppendLine($"            for (int i = 0; i < {name}Count; i++)");
                        sb.AppendLine("            {");
                        if (allMsgNames.Contains(csType))
                        {
                            sb.AppendLine($"                var item = new {csType}();");
                            sb.AppendLine($"                item.Deserialize(buffer);");
                            sb.AppendLine($"                {name}.Add(item);");
                        }
                        else
                        {
                            sb.AppendLine($"                {name}.Add(buffer.{GetReadMethod(csType)}());");
                        }
                        sb.AppendLine("            }");
                    }
                    else
                    {
                        if (allMsgNames.Contains(csType))
                        {
                            sb.AppendLine($"            {name} = new {csType}();");
                            sb.AppendLine($"            {name}.Deserialize(buffer);");
                        }
                        else
                        {
                            sb.AppendLine($"            {name} = buffer.{GetReadMethod(csType)}();");
                        }
                    }
                }
                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GetWriteMethod(string csType)
    {
        return csType switch
        {
            "uint"   => "WriteUInt",
            "int"    => "WriteInt",
            "ulong"  => "WriteULong",
            "long"   => "WriteLong",
            "float"  => "WriteFloat",
            "double" => "WriteDouble",
            "bool"   => "WriteBool",
            "string" => "WriteString",
            "byte[]" => "WriteByteArray",
            _        => $"WriteInt/*(unknown:{csType})*/",
        };
    }

    private static string GetReadMethod(string csType)
    {
        return csType switch
        {
            "uint"   => "ReadUInt",
            "int"    => "ReadInt",
            "ulong"  => "ReadULong",
            "long"   => "ReadLong",
            "float"  => "ReadFloat",
            "double" => "ReadDouble",
            "bool"   => "ReadBool",
            "string" => "ReadString",
            "byte[]" => "ReadByteArray",
            _        => $"ReadInt/*(unknown:{csType})*/",
        };
    }

    // 枚举值名转首字母大写
    private static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }
}
