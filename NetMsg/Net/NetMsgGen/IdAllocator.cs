using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NetMsgGen;

public readonly record struct IdEntry(int Id, string Name);

public static class IdAllocator
{
    public static List<IdEntry> Allocate(List<ProtoFile> files, Model model, string configPath, bool prune)
    {
        var existing = File.Exists(configPath) ? Parse(configPath) : new List<IdEntry>();
        var assigned = existing.ToDictionary(e => e.Name, e => e.Id);
        var usedIds = existing.Select(e => e.Id).ToHashSet();
        int maxCs = existing.Where(e => IsCs(e.Name)).Select(e => e.Id).DefaultIfEmpty(-1).Max();
        int maxSc = existing.Where(e => !IsCs(e.Name)).Select(e => e.Id).DefaultIfEmpty(9999).Max();

        var protocolNames = new List<string>();
        foreach (var file in files)
        {
            var ns = model.NamespaceOf(file);
            foreach (var msg in file.Messages.Where(m => Model.IsProtocol(m.Name)))
            {
                var full = ns + "." + msg.Name;
                protocolNames.Add(full);
                if (assigned.ContainsKey(full)) continue;

                int id;
                if (msg.ExplicitId is int explicitId)
                    id = explicitId;
                else if (msg.Name.StartsWith("SC") && assigned.TryGetValue(ns + ".CS" + msg.Name[2..], out var csId))
                    id = csId + 10000;
                else if (msg.Name.StartsWith("CS"))
                {
                    id = file.IdBase ?? maxCs + 1;
                    while (usedIds.Contains(id)) id++;
                }
                else
                {
                    id = maxSc + 1;
                    while (usedIds.Contains(id)) id++;
                }

                if (!usedIds.Add(id))
                    throw new Exception($"duplicate id {id} for {full}");

                assigned.Add(full, id);
                if (IsCs(full))
                    maxCs = Math.Max(maxCs, id);
                else
                    maxSc = Math.Max(maxSc, id);
            }
        }

        return assigned
            .Where(e => !prune || protocolNames.Contains(e.Key))
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
            var m = Regex.Match(line, @"^\s*(\d+)\s*=\s*([\w.]+)");
            if (m.Success)
                list.Add(new IdEntry(int.Parse(m.Groups[1].Value), m.Groups[2].Value));
        }
        return list;
    }

    private static bool IsCs(string fullName)
    {
        int dot = fullName.LastIndexOf('.');
        return fullName[(dot + 1)..].StartsWith("CS");
    }
}