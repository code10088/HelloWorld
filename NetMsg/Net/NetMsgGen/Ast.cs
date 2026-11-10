using System.Collections.Generic;

namespace NetMsgGen;

public sealed class ProtoFile
{
    public string Path;
    public string Name;
    public string Namespace;
    public string Package;
    public int? IdBase;
    public readonly List<MessageDecl> Messages = new();
    public readonly List<EnumDecl> Enums = new();
}

public sealed class MessageDecl
{
    public string Name;
    public int? ExplicitId;
    public readonly List<FieldDecl> Fields = new();
}

public sealed class FieldDecl
{
    public string Type;
    public string Name;
    public bool Repeated;
}

public sealed class EnumDecl
{
    public string Name;
    public readonly List<(string Name, int Value)> Values = new();
}