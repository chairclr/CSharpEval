using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace CSharpEval;

public class BasicCSharpEvaluator : ICSharpEvaluator
{
    public BasicScriptEnvironment ScriptEnvironment { get; private set; }

    private bool Disposed;

    public BasicCSharpEvaluator(IEnumerable<Assembly> assemblyImports, IEnumerable<string> usings)
    {
        ScriptEnvironment = new BasicScriptEnvironment(assemblyImports, usings);
    }

    public ScriptEvaluationResult Eval(string source)
    {
        Script continued = ScriptEnvironment.ScriptState.Script.ContinueWith(source);

        ImmutableArray<Diagnostic> diagnostics = continued.Compile();

        if (diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Any())
        {
            return new ScriptEvaluationResult(null, diagnostics, null);
        }

        try
        {
            ScriptEnvironment.ScriptState = SynchronousExecutor.Run(() => continued.RunFromAsync(ScriptEnvironment.ScriptState, catchException: (x) => true))!;

            if (ScriptEnvironment.ScriptState.Exception is not null)
            {
                return new ScriptEvaluationResult(null, diagnostics, ScriptEnvironment.ScriptState.Exception);
            }

            return new ScriptEvaluationResult(ScriptEnvironment.ScriptState.ReturnValue, diagnostics, null);
        }
        catch (Exception ex)
        {
            return new ScriptEvaluationResult(null, diagnostics, ex);
        }
    }

    public async Task<ScriptEvaluationResult> EvalAsync(string source, CancellationToken cancellationToken = default)
    {
        Script continued = ScriptEnvironment.ScriptState.Script.ContinueWith(source);

        ImmutableArray<Diagnostic> diagnostics = continued.Compile(cancellationToken);

        if (diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Any())
        {
            return new ScriptEvaluationResult(null, diagnostics, null);
        }

        try
        {
            ScriptEnvironment.ScriptState = await continued.RunFromAsync(ScriptEnvironment.ScriptState, catchException: (x) => true, cancellationToken: cancellationToken);

            if (ScriptEnvironment.ScriptState.Exception is not null)
            {
                return new ScriptEvaluationResult(null, diagnostics, ScriptEnvironment.ScriptState.Exception);
            }

            return new ScriptEvaluationResult(ScriptEnvironment.ScriptState.ReturnValue, diagnostics, null);
        }
        catch (Exception ex)
        {
            return new ScriptEvaluationResult(null, diagnostics, ex);
        }
    }
}
