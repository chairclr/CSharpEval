using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CSharpEval;

public record ScriptEvaluationResult(object? Result, ImmutableArray<Diagnostic> Diagnostics, Exception? Exception);
