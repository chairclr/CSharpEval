using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

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

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
