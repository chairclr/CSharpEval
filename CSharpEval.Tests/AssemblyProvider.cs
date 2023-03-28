using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEval.Tests;

public class AssemblyProvider
{
    private static string AssemblyContainingPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    public static readonly Assembly StaticAssembly = Assembly.LoadFile(Path.Combine(AssemblyContainingPath, "StaticTestAssembly.dll"));

    public static readonly Assembly DynamicAssembly;

    private static readonly AssemblyLoadContext DynamicAssemblyLoadContext = new AssemblyLoadContext("DynamicLoadContext");

    static AssemblyProvider()
    {
        using FileStream fs = new FileStream(Path.Combine(AssemblyContainingPath, "DynamicTestAssembly.dll"), FileMode.Open);

        DynamicAssembly = DynamicAssemblyLoadContext.LoadFromStream(fs);
    }
}
