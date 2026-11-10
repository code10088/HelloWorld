using System;
using System.Collections.Generic;

namespace NetMsgGen;

public sealed class Model
{
    public readonly List<ProtoFile> Files;
    public readonly HashSet<string> EnumNames = new();

    public Model(List<ProtoFile> files)
    {
        Files = files;
        foreach (var file in files)
            foreach (var e in file.Enums)
                EnumNames.Add(e.Name);
    }

    public string NamespaceOf(ProtoFile file) => file.Namespace ?? file.Package ?? "Message";

    public static bool IsProtocol(string name) => name.StartsWith("CS") || name.StartsWith("SC");
}