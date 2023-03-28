using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace CSharpEval.Tests.Unit;

public class FullEvaluatorTests
{
    [TestCase("6 + 4", ExpectedResult = 10)]
    [TestCase("48 * 3", ExpectedResult = 144)]
    [TestCase("\"Hello\" + \" \" + \"World\"", ExpectedResult = "Hello World")]
    public object? TestEvalAsyncBasic(string code)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

        return evaluator.Eval(code).Result;
    }

    [TestCase("throw new System.Exception();")]
    public void TestEvalAsyncException(string code)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { typeof(Exception).Assembly }, Enumerable.Empty<string>());

        Assert.That(evaluator.Eval(code).Exception, Is.InstanceOf<Exception>());
    }

    [TestCase("StaticExportsClass.TestFunction(\"Hello\")", ExpectedResult = 5)]
    [TestCase("StaticExportsClass.TestFunction(\"Hello World\")", ExpectedResult = 11)]
    [TestCase("StaticExportsClass.TestFunction(\"\")", ExpectedResult = 0)]
    public object? TestEvalStaticReference(string code)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.StaticAssembly }, new string[] { "StaticTestAssembly" });

        return evaluator.Eval(code).Result;
    }

    [TestCase("DynamicExportsClass.TestFunction(\"Hello\")", ExpectedResult = 5)]
    [TestCase("DynamicExportsClass.TestFunction(\"Hello World\")", ExpectedResult = 11)]
    [TestCase("DynamicExportsClass.TestFunction(\"\")", ExpectedResult = 0)]
    public object? TestEvalDynamicReference(string code)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.DynamicAssembly }, new string[] { "DynamicTestAssembly" });

        return evaluator.Eval(code).Result;
    }

    [TestCase("2 = 3")]
    [TestCase("}")]
    public void TestEvalAsyncCompilationError(string code)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

        ScriptEvaluationResult result = evaluator.Eval(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Diagnostics.First().Severity, Is.EqualTo(DiagnosticSeverity.Error));
        });
    }

    [Test]
    public void TestEvalSync()
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { typeof(Environment).Assembly, typeof(Task).Assembly }, Enumerable.Empty<string>());

        string code = """
                      await System.Threading.Tasks.Task.Delay(500);
                      System.Environment.CurrentManagedThreadId
                      """;

        Assert.That((int)evaluator.Eval(code).Result!, Is.EqualTo(Environment.CurrentManagedThreadId));
    }

    [TestCase("Consol", 6, 'l', ExpectedResult = "Console")]
    [TestCase("for (int i = 0; i < 5; i++) { Console.WriteLi }", 45, 'r', ExpectedResult = "WriteLine")]
    public string TestBasicInsertionCompletion(string code, int caretPosition, char insertedCharacter)
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { typeof(Console).Assembly }, new string[] { "System" });

        ImmutableArray<CompletionItem> completions = evaluator.GetCompletionsAsync(code, caretPosition, CompletionTrigger.CreateInsertionTrigger(insertedCharacter)).Result;

        return completions.First().DisplayText;
    }

    [Test]
    public void TestImportStaticCompletion()
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.StaticAssembly }, new string[] { "StaticTestAssembly" });

        ImmutableArray<CompletionItem> completions = evaluator.GetCompletionsAsync("StaticExportsCla", 16, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completions.First().DisplayText, Is.EqualTo("StaticExportsClass"));
    }

    [Test]
    public void TestUsingAfterCreationStaticCompletion()
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.StaticAssembly }, new string[] {  });

        ImmutableArray<CompletionItem> completions = evaluator.GetCompletionsAsync("StaticExportsCla", 16, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completions.IsEmpty);

        evaluator.Eval("using StaticTestAssembly;").FailIfErrors();

        ImmutableArray<CompletionItem> completionsAfterUsing = evaluator.GetCompletionsAsync("StaticExportsCla", 16, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completionsAfterUsing.First().DisplayText, Is.EqualTo("StaticExportsClass"));
    }

    [Test]
    public void TestImportDynamicCompletion()
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.DynamicAssembly }, new string[] { "DynamicTestAssembly" });

        ImmutableArray<CompletionItem> completions = evaluator.GetCompletionsAsync("DynamicExportsCla", 17, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completions.First().DisplayText, Is.EqualTo("DynamicExportsClass"));
    }

    [Test]
    public void TestUsingAfterCreationDynamicCompletion()
    {
        using FullCSharpEvaluator evaluator = new FullCSharpEvaluator(new Assembly[] { AssemblyProvider.DynamicAssembly }, new string[] {  });

        ImmutableArray<CompletionItem> completions = evaluator.GetCompletionsAsync("DynamicExportsCla", 17, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completions.IsEmpty);

        evaluator.Eval("using DynamicTestAssembly;").FailIfErrors();

        ImmutableArray<CompletionItem> completionsAfterUsing = evaluator.GetCompletionsAsync("DynamicExportsCla", 17, CompletionTrigger.CreateInsertionTrigger('a')).Result;

        Assert.That(completionsAfterUsing.First().DisplayText, Is.EqualTo("DynamicExportsClass"));
    }
}