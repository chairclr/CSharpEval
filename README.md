<h1 align="center">
C# Eval
</h1>
<p align="center">
C# Eval is a library for running and debugging your code at runtime
</p>
<br>

<h2>
Examples
</h2>

<h3>
Basic evaluation
</h3>

```cs
// Create a basic evaluation context with no references or usings
BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

evaluator.Eval("int x = 47;");
evaluator.Eval("int y = 3;");

Console.WriteLine(evaluator.Eval("(x + y) / 2").Result); // Prints 25
```

<h3>
Importing a library and using
</h3>

```cs
Assembly[] references = new Assembly[] { typeof(System.Console).Assembly };
string[] usings = new string[] { "System" };

// Create a basic evaluation context with a reference to the System library and a using System
BasicCSharpEvaluator evaluator = new BasicCSharpEvaluator(references, usings);

evaluator.Eval("Console.WriteLine(\"Hello From Eval\")");
```
