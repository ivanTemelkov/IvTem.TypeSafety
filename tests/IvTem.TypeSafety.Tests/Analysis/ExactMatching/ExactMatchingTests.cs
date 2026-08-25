using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.ExactMatching;

public sealed class ExactMatchingTests
{
    [Fact]
    public void ExactForbiddenTypeArgumentReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.Exception>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("System.Exception", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowExactTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DerivedTypeArgumentDoesNotMatchExactForbiddenBaseType()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NullableReferenceAnnotationDoesNotBypassExactMatch()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
#nullable enable
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(string))] T>
{
}

internal sealed class Consumer
{
    private Data<string>? nonNullableValue;
    private Data<string?>? nullableValue;
}
""");

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
        Assert.All(diagnostics, diagnostic => Assert.Equal("IVTS001", diagnostic.Id));
    }

    [Fact]
    public void DynamicMatchesExactObjectRestriction()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(object))] T>
{
}

internal sealed class Consumer
{
    private Data<dynamic>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
    }

    [Fact]
    public void NullableValueTypeDoesNotMatchUnderlyingExactValueType()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(int))] T>
{
}

internal sealed class Consumer
{
    private Data<int?>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GenericParameterConstraintDoesNotProveExactTypeIdentity()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
    where T : System.Exception
{
    private Data<T>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void AliasAndFrameworkTypeNameMatchBySemanticIdentity()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(int))] T>
{
}

internal sealed class OtherData<[DisallowExactTypes(typeof(System.Int32))] T>
{
}

internal sealed class Consumer
{
    private Data<System.Int32>? value;
    private OtherData<int>? otherValue;
}
""");

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
        Assert.All(diagnostics, diagnostic => Assert.Equal("IVTS001", diagnostic.Id));
    }
}
