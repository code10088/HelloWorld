using System.Collections.Generic;
using System.Linq;

namespace NetMsgGen;

public static class AConfigEmitter
{
    public static string Emit(List<IdEntry> entries)
    {
        var cs = entries.Where(e => ClassName(e.Name).StartsWith("CS")).OrderBy(e => e.Id).ToList();
        var sc = entries.Where(e => !ClassName(e.Name).StartsWith("CS")).OrderBy(e => e.Id).ToList();

        var lines = new List<string> { "//Id=(Namespace.)CalssName" };
        lines.AddRange(cs.Select(e => $"{e.Id}\t\t= {e.Name}"));
        if (cs.Count > 0 && sc.Count > 0) lines.Add("");
        lines.AddRange(sc.Select(e => $"{e.Id}\t= {e.Name}"));
        return string.Join("\r\n", lines);
    }

    private static string ClassName(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
}