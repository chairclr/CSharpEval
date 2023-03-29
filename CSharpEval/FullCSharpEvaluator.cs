using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
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

    public async Task<ImmutableArray<CompletionItem>> GetCompletionsAsync(string source, int caretPosition, CompletionTrigger completionTrigger, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(source))
        {
            return ImmutableArray<CompletionItem>.Empty;
        }
            
        ScriptEnvironment.UpdateTextOnly(source);

        CompletionService? completionService = CompletionService.GetService(ScriptEnvironment.ScriptDocument);

        if (completionService is null)
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        if (!ShouldTriggerCompletion(completionTrigger))
        {
            return ImmutableArray<CompletionItem>.Empty;
        }

        CompletionList completions = await completionService.GetCompletionsAsync(ScriptEnvironment.ScriptDocument, caretPosition, trigger: default, cancellationToken: cancellationToken);

        if (completions.Span.Length == 0)
        {
            return completions.ItemsList.ToImmutableArray();
        }

        string filterText = source.Substring(completions.Span.Start, completions.Span.Length);

        ImmutableArray<CompletionItem> filteredItems = completionService.FilterItems(ScriptEnvironment.ScriptDocument, completions.ItemsList.ToImmutableArray(), filterText);

        return filteredItems.Sort((x, y) => CompareCompletionItems(filterText, x, y));
    }

    private bool ShouldTriggerCompletion(CompletionTrigger completionTrigger)
    {
        if (completionTrigger.Kind == CompletionTriggerKind.Insertion || completionTrigger.Kind == CompletionTriggerKind.Deletion)
        {
            if (char.IsLetterOrDigit(completionTrigger.Character) || completionTrigger.Character == '.')
            {
                return true;
            }
        }

        return false;
    }

    private int CompareCompletionItems(string filterText, CompletionItem x, CompletionItem y)
    {
        if (filterText == x.SortText)
        {
            return 1;
        }

        int MinimumDistance(CompletionItem item)
        {
            return StringCompare.StringDistance(filterText, item.SortText);
        }

        return MinimumDistance(x).CompareTo(MinimumDistance(y));
    }

    /// <summary>
    /// Applies a completion item to the source text
    /// </summary>
    /// <param name="source">Source code/text</param>
    /// <param name="commitCharacter">The character added to trigger the completion. This is null when the [TAB] or [ENTER] keys were used</param>
    /// <returns>New source code, new caret position</returns>
    public async Task<(string, int)> ApplyCompletionAsync(string source, CompletionItem item, int caretPosition, char? commitCharacter)
    {
        ScriptEnvironment.UpdateTextOnly(source);

        CompletionService? completionService = CompletionService.GetService(ScriptEnvironment.ScriptDocument);

        if (completionService is null) 
        {
            return (source, caretPosition);
        }

        CompletionChange change = await completionService.GetChangeAsync(ScriptEnvironment.ScriptDocument, item, commitCharacter);

        string insertedText = change.TextChange.NewText ?? "";

        string newSource = source.Remove(change.TextChange.Span.Start, change.TextChange.Span.Length).Insert(change.TextChange.Span.Start, insertedText);

        int newCaretPosition;

        if (change.NewPosition.HasValue)
        {
            newCaretPosition = change.NewPosition.Value;
        }
        else
        {
            newCaretPosition = caretPosition + insertedText.Length - change.TextChange.Span.Length;
        }

        return (newSource, newCaretPosition);
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
