namespace CSharpEval;

public interface ICSharpEvaluator
{
    public ScriptEvaluationResult Eval(string source);

    public Task<ScriptEvaluationResult> EvalAsync(string source, CancellationToken cancellationToken);
}
