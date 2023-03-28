using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CSharpEval.Tests.Unit;

public class BasicEvaluatorTests
{
    [TestCase("6 + 4", ExpectedResult = 10)]
    [TestCase("48 * 3", ExpectedResult = 144)]
    [TestCase("\"Hello\" + \" \" + \"World\"", ExpectedResult = "Hello World")]
    public object? TestEvalBasic(string code)
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

        return evaluator.Eval(code).Result;
    }

    [TestCase("throw new System.Exception();")]
    public void TestEvalException(string code)
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(new Assembly[] { typeof(Exception).Assembly }, Enumerable.Empty<string>());

        Assert.That(evaluator.Eval(code).Exception, Is.InstanceOf<Exception>());
    }

    [TestCase("StaticExportsClass.TestFunction(\"Hello\")", ExpectedResult = 5)]
    [TestCase("StaticExportsClass.TestFunction(\"Hello World\")", ExpectedResult = 11)]
    [TestCase("StaticExportsClass.TestFunction(\"\")", ExpectedResult = 0)]
    public object? TestEvalStaticReference(string code)
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(new Assembly[] { AssemblyProvider.StaticAssembly }, new string[] { "StaticTestAssembly" });

        return evaluator.Eval(code).Result;
    }

    [TestCase("DynamicExportsClass.TestFunction(\"Hello\")", ExpectedResult = 5)]
    [TestCase("DynamicExportsClass.TestFunction(\"Hello World\")", ExpectedResult = 11)]
    [TestCase("DynamicExportsClass.TestFunction(\"\")", ExpectedResult = 0)]
    public object? TestEvalDynamicReference(string code)
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(new Assembly[] { AssemblyProvider.DynamicAssembly }, new string[] { "DynamicTestAssembly" });

        return evaluator.Eval(code).Result;
    }

    [TestCase("2 = 3")]
    [TestCase("}")]
    public void TestEvalCompilationError(string code)
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

        ScriptEvaluationResult result = evaluator.Eval(code);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.Null);
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(DiagnosticSeverity.Error));
        });
    }

    [Test]
    public void TestEvalSync()
    {
        BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(new Assembly[] { typeof(Environment).Assembly, typeof(Task).Assembly }, Enumerable.Empty<string>());

        string code = """
                      await System.Threading.Tasks.Task.Delay(500);
                      System.Environment.CurrentManagedThreadId
                      """;

        Assert.That((int)evaluator.Eval(code).Result!, Is.EqualTo(Environment.CurrentManagedThreadId));
    }
}