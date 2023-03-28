using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpEval.Tests.Unit;
using Microsoft.CodeAnalysis;

namespace CSharpEval.Tests;

public static class ScriptExtensions
{
    public static void FailIfErrors(this ScriptEvaluationResult result)
    {
        if (result.Diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
        {
            Assert.Fail();
        }
    }
}
