using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.GenericMethods;

public sealed class GenericMethodUseSiteTests
{
    [Fact]
    public void ExplicitGenericMethodArgumentViolationReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Create<System.Exception>();
    }

    private static void Create<[DisallowTypes(typeof(System.Exception))] T>()
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("System.Exception", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void InferredGenericMethodArgumentViolationReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use(System.InvalidOperationException value)
    {
        Create(value);
    }

    private static void Create<[DisallowTypes(typeof(System.Exception))] T>(T value)
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("System.InvalidOperationException", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void InferredAllowedGenericMethodArgumentDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use(string value)
    {
        Create(value);
    }

    private static void Create<[DisallowTypes(typeof(System.Exception))] T>(T value)
    {
    }
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MethodGroupAssignedToDelegateReportsClosedGenericArgumentViolation()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System;
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Action<System.Exception> action = Create;
    }

    private static void Create<[DisallowTypes(typeof(System.Exception))] T>(T value)
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("System.Exception", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void GenericLocalFunctionExplicitArgumentViolationReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Create<System.Exception>();

        static void Create<[DisallowTypes(typeof(System.Exception))] T>()
        {
        }
    }
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void GenericLocalFunctionInferredArgumentViolationReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use(System.InvalidOperationException value)
    {
        Create(value);

        static void Create<[DisallowTypes(typeof(System.Exception))] T>(T item)
        {
        }
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Contains("System.InvalidOperationException", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleRestrictionsReportOneDiagnosticPerMethodTypeArgument()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Create<System.InvalidOperationException>();
    }

    private static void Create<[DisallowTypes(typeof(System.Exception), typeof(System.SystemException))] T>()
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.SystemException)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitGenericMethodArgumentDiagnosticUsesTypeArgumentLocation()
    {
        const string offendingText = "System.Exception";
        var source = """
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Create<System.Exception>();
    }

    private static void Create<[DisallowExactTypes(typeof(System.Exception))] T>()
    {
    }
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source));
        var spanText = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);

        Assert.Equal(offendingText, spanText);
    }

    [Fact]
    public void MethodGroupReportsOnlyOneDiagnosticForDelegateConversion()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System;
using IvTem.TypeSafety;

internal sealed class Consumer
{
    private void Use()
    {
        Action<System.Exception> action = Create<System.Exception>;
    }

    private static void Create<[DisallowTypes(typeof(System.Exception))] T>(T value)
    {
    }
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }
}
