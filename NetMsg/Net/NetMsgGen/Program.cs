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
        var opt = Options.Parse(args);
        var files = ProtoParser.ParseDirectory(opt.Proto);
        var model = new Model(files);
        var entries = IdAllocator.Allocate(files, model, opt.Config, opt.Prune);

        var outputs = new Dictionary<string, string>();
        foreach (var file in files)
            outputs[Path.Combine(opt.Out, file.Name + ".cs")] = CSharpEmitter.Emit(file, model);
        outputs[opt.Config] = AConfigEmitter.Emit(entries);

        if (opt.Check)
        {
            var diffs = outputs.Where(o => !File.Exists(o.Key) || File.ReadAllText(o.Key) != o.Value).ToList();
            foreach (var d in diffs) Console.WriteLine("diff: " + d.Key);
            return diffs.Count == 0 ? 0 : 1;
        }

        Directory.CreateDirectory(opt.Out);
        foreach (var o in outputs)
        {
            Console.WriteLine("write: " + o.Key);
            File.WriteAllText(o.Key, o.Value, new UTF8Encoding(false));
        }
        return 0;
    }
}

public sealed class Options
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
                case "--proto": opt.Proto = args[++i]; break;
                case "--out": opt.Out = args[++i]; break;
                case "--config": opt.Config = args[++i]; break;
                case "--check": opt.Check = true; break;
                case "--prune": opt.Prune = true; break;
            }
        }
        return opt;
    }

    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Proto")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? Directory.GetCurrentDirectory();
    }
}