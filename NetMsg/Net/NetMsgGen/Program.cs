using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetMsgGen;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        var opt = Options.Parse(args);
        var files = ProtoParser.ParseDirectory(opt.Proto);
        var model = new Model(files);
        var list = IdAllocator.Allocate(files, model, opt.Config, opt.Prune);

        var outputs = new Dictionary<string, string>();
        foreach (var file in files) outputs[Path.Combine(opt.Out, file.Name + ".cs")] = CSharpEmitter.Emit(file, model);
        outputs[opt.Config] = AConfigEmitter.Emit(list);

        if (opt.Check)
        {
            var diffs = outputs.Where(o => !File.Exists(o.Key) || File.ReadAllText(o.Key) != o.Value).ToList();
            foreach (var item in diffs) Console.WriteLine("diff: " + item.Key);
            return diffs.Count == 0 ? 0 : 1;
        }

        Directory.CreateDirectory(opt.Out);
        foreach (var item in outputs)
        {
            Console.WriteLine("write: " + item.Key);
            File.WriteAllText(item.Key, item.Value, new UTF8Encoding(false));
        }
        return 0;
    }
}

public class Options
{
    public string Proto;
    public string Out;
    public string Config;
    public bool Check;
    public bool Prune;

    public static Options Parse(string[] args)
    {
        var root = FindRoot();
        var opt = new Options
        {
            Proto = Path.Combine(root, "Proto"),
            Out = Path.Combine(root, "Net", "Out"),
            Config = Path.Combine(root, "Proto", "AConfig.txt"),
        };
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--proto": opt.Proto = Value(args, ref i); break;
                case "--out": opt.Out = Value(args, ref i); break;
                case "--config": opt.Config = Value(args, ref i); break;
                case "--check": opt.Check = true; break;
                case "--prune": opt.Prune = true; break;
                default: throw new Exception("unknown argument " + args[i]);
            }
        }
        return opt;
    }

    private static string Value(string[] args, ref int i)
    {
        if (i + 1 >= args.Length) throw new Exception("missing value for " + args[i]);
        return args[++i];
    }

    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Proto"))) dir = Path.GetDirectoryName(dir);
        return dir ?? Directory.GetCurrentDirectory();
    }
}

public static class AConfigEmitter
{
    public static string Emit(List<IdEntry> list)
    {
        var cs = list.Where(e => ClassName(e.Name).StartsWith("CS")).OrderBy(e => e.Id).ToList();
        var sc = list.Where(e => !ClassName(e.Name).StartsWith("CS")).OrderBy(e => e.Id).ToList();

        var lines = new List<string> { "//Id=(Namespace.)CalssName" };
        lines.AddRange(cs.Select(e => $"{e.Id}\t\t= {e.Name}"));
        if (cs.Count > 0 && sc.Count > 0) lines.Add("");
        lines.AddRange(sc.Select(e => $"{e.Id}\t= {e.Name}"));
        return string.Join("\r\n", lines);
    }

    private static string ClassName(string fullName) => fullName[(fullName.LastIndexOf('.') + 1)..];
}