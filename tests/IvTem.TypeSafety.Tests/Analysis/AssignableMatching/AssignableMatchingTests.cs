using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.AssignableMatching;

public sealed class AssignableMatchingTests
{
    [Fact]
    public void SameTypeReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.Exception>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DerivedClassReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void InterfaceImplementationReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.IDisposable))] T>
{
}

internal sealed class Resource : System.IDisposable
{
    public void Dispose()
    {
    }
}

internal sealed class Consumer
{
    private Data<Resource>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void GenericVarianceReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System.Collections.Generic;
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(IEnumerable<object>))] T>
{
}

internal sealed class Consumer
{
    private Data<IEnumerable<string>>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void ArrayCovarianceReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(object[]))] T>
{
}

internal sealed class Consumer
{
    private Data<string[]>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void ValueTypesReportForForbiddenValueType()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.ValueType))] T>
{
}

internal sealed class Consumer
{
    private Data<int>? value;
    private Data<System.DateTime>? otherValue;
}
""");

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void ValueTypeInterfaceRelationshipReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.IComparable))] T>
{
}

internal sealed class Consumer
{
    private Data<int>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void RefLikeTypeInterfaceRelationshipReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.IDisposable))] T>
    where T : allows ref struct
{
}

internal ref struct Resource
    : System.IDisposable
{
    public void Dispose()
    {
    }
}

internal sealed class Consumer
{
    private Data<Resource>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void UserDefinedImplicitConversionDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(Target))] T>
{
}

internal sealed class Source
{
    public static implicit operator Target(Source source)
        => new Target();
}

internal sealed class Target
{
}

internal sealed class Consumer
{
    private Data<Source>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UserDefinedExplicitConversionDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(Target))] T>
{
}

internal sealed class Source
{
    public static explicit operator Target(Source source)
        => new Target();
}

internal sealed class Target
{
}

internal sealed class Consumer
{
    private Data<Source>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NumericConversionDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(long))] T>
{
}

internal sealed class Consumer
{
    private Data<int>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NestedTypeArgumentsAreNotInspected()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System.Collections.Generic;
using System.Threading.Tasks;
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<List<System.Exception>>? value;
    private Data<Task<System.Exception>>? otherValue;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GenericParameterWithDirectClassConstraintReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
    where T : System.Exception
{
    private Data<T>? value;
}
""");

        Assert.Single(diagnostics);
    }

    [Fact]
    public void GenericParameterWithoutProvingConstraintDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
{
    private Data<T>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GenericParameterConstraintChainDoesNotReportDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T, U>
    where T : U
    where U : System.Exception
{
    private Data<T>? value;
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MultipleAssignableMatchesReportOneDiagnosticForTypeArgument()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception), typeof(System.SystemException))] T>
{
}

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.SystemException)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
