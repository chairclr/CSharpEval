using System.Collections.Immutable;
using System.Composition.Hosting;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;

namespace CSharpEval;

public class FullScriptEnvironment : IDisposable
{
    private readonly CompositionHost CompositionHost;

    public readonly MefHostServices ScriptHost;

    public readonly AdhocWorkspace ScriptWorkspace;

    public Solution ScriptSolution => ScriptWorkspace.CurrentSolution;

    public ScriptState ScriptState;

    private readonly InteractiveAssemblyLoader AssemblyLoader;

    private Project ScriptProject;

    private Document ScriptDocument;

    private bool Disposed;

#pragma warning disable CS8618 // False-positive, Update("") will set ScriptDocument to non-null value
    public FullScriptEnvironment(IEnumerable<Assembly> assemblyImports, IEnumerable<string> usings)
#pragma warning restore CS8618
    {
        CompositionHost = new ContainerConfiguration().WithAssemblies(assemblyImports.Concat(MefHostServices.DefaultAssemblies)).CreateContainer();

        ScriptHost = new MefHostServices(CompositionHost);

        ScriptWorkspace = new AdhocWorkspace(ScriptHost);

        IEnumerable<PortableExecutableReference> metadataReferences = assemblyImports.Select(x => ReferenceProvider.GetBestPEReference(x));

        ProjectInfo scriptProjectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "ScriptProject", "Script", LanguageNames.CSharp)
            .WithMetadataReferences(metadataReferences)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script));

        ScriptProject = ScriptWorkspace.AddProject(scriptProjectInfo);

        Update("");

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

    public void Update(string source) => Update(SourceText.From(source));

    public void Update(SourceText source)
    {
        UpdateUsingsWithSource(source);

        ScriptDocument = ScriptProject.AddDocument("Script", source);

        DocumentId scriptDocumentId = ScriptDocument.Id;

        if (!ScriptWorkspace.TryApplyChanges(ScriptDocument.Project.Solution))
        {
            throw new Exception("Could not apply changes.");
        }

        ScriptDocument = ScriptSolution.GetDocument(scriptDocumentId)!;

        ScriptProject = ScriptDocument.Project;
    }

    private void UpdateUsingsWithSource(SourceText source)
    {
        CSharpCompilationOptions compliation = (CSharpCompilationOptions)ScriptProject.CompilationOptions!;

        ImmutableArray<string> currentUsings = compliation.Usings;

        ScriptDocument = ScriptProject.AddDocument("Script", source);

        if (ScriptDocument.TryGetSyntaxTree(out SyntaxTree? syntaxTree))
        {
            foreach (UsingDirectiveSyntax usingSyntax in syntaxTree.GetRoot().DescendantNodes().Where(x => x is UsingDirectiveSyntax).Cast<UsingDirectiveSyntax>())
            {
                string import = usingSyntax.Name.ToString();

                if (!currentUsings.Contains(import))
                {
                    currentUsings = currentUsings.Add(import);
                }
            }
        }

        ScriptProject = ScriptProject.WithCompilationOptions(compliation.WithUsings(currentUsings));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                CompositionHost.Dispose();

                ScriptWorkspace.Dispose();

                AssemblyLoader.Dispose();
            }

            ScriptState = null;

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}