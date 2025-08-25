using IngameDebugConsole;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Reflection;

public static class TestCMD
{
    private static CSharpCodeProvider provider;
    private static CompilerParameters parameters;

    [ConsoleMethod("Test", "Test")]
    public static void Execute(string code)
    {
        if (provider == null)
        {
            provider = new CSharpCodeProvider();
            parameters = new CompilerParameters()
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
            };
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic && !assembly.Location.Contains("mscorlib")) parameters.ReferencedAssemblies.Add(assembly.Location);
            }
        }

        CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
        if (results.Errors.HasErrors)
        {
            foreach (CompilerError error in results.Errors) GameDebug.LogError(error.ErrorText);
        }
        else
        {
            Assembly assembly = results.CompiledAssembly;
            object obj = assembly.CreateInstance("TestClass");
            MethodInfo mi = obj.GetType().GetMethod("Execute");
            mi.Invoke(obj, null);
        }
    }
}
