using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetMsgGen;

public class IdEntry
{
    public int Id;
    public string Name;

    public IdEntry(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

public static class IdAllocator
{
    public static List<IdEntry> Allocate(List<ProtoFile> files, Model model, string configPath, bool prune)
    {
        var existing = File.Exists(configPath) ? Parse(configPath) : new List<IdEntry>();

        var assigned = new Dictionary<string, int>();
        foreach (var item in existing)
        {
            if (!assigned.TryAdd(item.Name, item.Id))
                throw new Exception("duplicate message in AConfig.txt: " + item.Name);
        }

        var used = existing.Select(e => e.Id).ToHashSet();
        var maxCs = existing.Where(e => IsCs(e.Name)).Select(e => e.Id).DefaultIfEmpty(-1).Max();
        var maxSc = existing.Where(e => !IsCs(e.Name)).Select(e => e.Id).DefaultIfEmpty(9999).Max();

        var names = new List<string>();
        foreach (var file in files)
        {
            var ns = model.NamespaceOf(file);
            foreach (var msg in file.Messages.Where(m => Model.IsProtocol(m.Name)))
            {
                var full = ns + "." + msg.Name;
                names.Add(full);
                if (assigned.ContainsKey(full)) continue;

                int id;
                if (msg.ExplicitId is int explicitId) id = explicitId;
                else if (msg.Name.StartsWith("SC") && assigned.TryGetValue(ns + ".CS" + msg.Name[2..], out var csId)) id = csId + 10000;
                else if (msg.Name.StartsWith("CS"))
                {
                    id = file.IdBase ?? maxCs + 1;
                    while (used.Contains(id)) id++;
                }
                else
                {
                    id = maxSc + 1;
                    while (used.Contains(id)) id++;
                }

                if (id < 0 || id > ushort.MaxValue)
                    throw new Exception($"message id out of range 0..{ushort.MaxValue}: {full}={id}");
                if (!used.Add(id))
                    throw new Exception($"duplicate id {id} for {full}");

                assigned.Add(full, id);
                if (IsCs(full)) maxCs = Math.Max(maxCs, id);
                else maxSc = Math.Max(maxSc, id);
            }
        }

        return assigned
            .Where(e => !prune || names.Contains(e.Key))
            .Select(e => new IdEntry(e.Value, e.Key))
            .OrderBy(e => IsCs(e.Name) ? 0 : 1)
            .ThenBy(e => e.Id)
            .ToList();
    }

    private static List<IdEntry> Parse(string path)
    {
        var list = new List<IdEntry>();
        foreach (var line in File.ReadAllText(path).Split('\n'))
        {
            var match = Regex.Match(line, @"^\s*(\d+)\s*=\s*([\w.]+)");
            if (match.Success) list.Add(new IdEntry(int.Parse(match.Groups[1].Value), match.Groups[2].Value));
        }
        return list;
    }

    private static bool IsCs(string fullName)
    {
        var index = fullName.LastIndexOf('.');
        return fullName[(index + 1)..].StartsWith("CS");
    }
}