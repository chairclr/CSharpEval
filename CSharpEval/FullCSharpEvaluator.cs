using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace CSharpEval;


public class FullCSharpEvaluator : ICSharpEvaluator, IDisposable
{
    public FullScriptEnvironment ScriptEnvironment { get; private set; }

    private bool Disposed;

    public FullCSharpEvaluator(IEnumerable<Assembly> assemblyImports, IEnumerable<string> usings)
    {
        ScriptEnvironment = new FullScriptEnvironment(assemblyImports, usings);
    }

    public ScriptEvaluationResult Eval(string source)
    {
        ScriptEnvironment.Update(source);

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
        ScriptEnvironment.Update(source);

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

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                ScriptEnvironment.Dispose();
            }

            ScriptEnvironment = null;
            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
