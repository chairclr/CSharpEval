using System.Reflection;

namespace CSharpEval.Tests.Unit;

public class ScriptEnvironmentTests
{
    [Test]
    public void CreateEnvironmentFromSingleStaticAssembly()
    {
        using FullScriptEnvironment scriptEnvironment = new FullScriptEnvironment(new Assembly[] { AssemblyProvider.StaticAssembly }, new string[] { "StaticTestAssembly" });

        Assert.That(scriptEnvironment.ScriptState, Is.Not.Null);

        scriptEnvironment.Update("StaticExportsClass.TestFunction(\"Hi\");");
    }

    [Test]
    public void CreateEnvironmentFromSingleDynamicAssembly()
    {
        using FullScriptEnvironment scriptEnvironment = new FullScriptEnvironment(new Assembly[] { AssemblyProvider.DynamicAssembly }, new string[] { "DynamicTestAssembly" });

        Assert.That(scriptEnvironment.ScriptState, Is.Not.Null);

        scriptEnvironment.Update("DynamicExportsClass.TestFunction(\"Hi\");");
    }

    [Test]
    public void CreateEnvironmentFromStaticAndDynamicAssemblies()
    {
        using FullScriptEnvironment scriptEnvironment = new FullScriptEnvironment(new Assembly[] { AssemblyProvider.StaticAssembly, AssemblyProvider.DynamicAssembly }, new string[] { "StaticTestAssembly", "DynamicTestAssembly" });

        Assert.That(scriptEnvironment.ScriptState, Is.Not.Null);

        scriptEnvironment.Update("StaticExportsClass.TestFunction(\"Hi\");");
        scriptEnvironment.Update("DynamicExportsClass.TestFunction(\"Hi\");");
    }
}
