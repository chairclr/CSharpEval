using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace CSharpEval;

public class BasicScriptEnvironment
{
    public ScriptState ScriptState;

    private readonly InteractiveAssemblyLoader AssemblyLoader;

    public BasicScriptEnvironment(IEnumerable<Assembly> assemblyImports, IEnumerable<string> usings)
    {
        IEnumerable<PortableExecutableReference> metadataReferences = assemblyImports.Select(x => ReferenceProvider.GetBestPEReference(x));

        ScriptOptions scriptOptions = ScriptOptions.Default
            .AddReferences(metadataReferences)
            .AddImports(usings);

        AssemblyLoader = new InteractiveAssemblyLoader();

        foreach (Assembly assembly in assemblyImports)
        {
            AssemblyLoader.RegisterDependency(assembly);
        }

        ScriptState = CSharpScript.Create("", scriptOptions, assemblyLoader: AssemblyLoader).RunAsync().Result;
    }
}