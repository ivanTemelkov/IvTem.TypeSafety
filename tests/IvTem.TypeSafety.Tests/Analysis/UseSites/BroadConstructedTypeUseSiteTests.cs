using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.UseSites;

public sealed class BroadConstructedTypeUseSiteTests
{
    [Fact]
    public void FieldPropertyParameterAndReturnTypeUsesReportDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.Exception>? field;

    private Data<System.InvalidOperationException>? Property { get; set; }

    private Data<System.SystemException> Create(Data<System.ApplicationException> value)
        => value;
}
""");

        Assert.Equal(4, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void TypeOfObjectCreationAndDefaultUsesReportDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System;
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private void Use()
    {
        _ = typeof(Data<System.Exception>);
        _ = new Data<System.InvalidOperationException>();
        _ = default(Data<System.SystemException>);
    }
}
""");

        Assert.Equal(3, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void ExplicitObjectCreationReportsOnlyOneDiagnosticAcrossSyntaxAndOperationActions()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private void Use()
    {
        _ = new Data<System.InvalidOperationException>();
    }
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void TargetTypedNewReportsOperationUseWhenConstructedTargetTypeIsRestricted()
    {
        const string targetTypedNew = "new()";
        var source = """
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private static Data<System.InvalidOperationException> Create()
        => new();
}
""";

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001")
            .ToArray();

        Assert.Equal(2, diagnostics.Length);
        Assert.Contains(diagnostics, diagnostic => GetSourceText(source, diagnostic).Equals(targetTypedNew, StringComparison.Ordinal));
    }

    [Fact]
    public void BaseAndInterfaceDeclarationsReportDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal class Base<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal interface IContract<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Derived
    : Base<System.InvalidOperationException>,
      IContract<System.ApplicationException>
{
}
""");

        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void CastIsPatternAndAsUsesReportDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private void Use(object value)
    {
        _ = (Data<System.InvalidOperationException>)value;
        _ = value is Data<System.ApplicationException>;
        _ = value as Data<System.SystemException>;
    }
}
""");

        Assert.Equal(3, diagnostics.Count(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void AttributeTypeOfReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System;
using IvTem.TypeSafety;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class UsesTypeAttribute : Attribute
{
    public UsesTypeAttribute(Type type)
    {
    }
}

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

[UsesType(typeof(Data<System.InvalidOperationException>))]
internal sealed class Consumer
{
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void ConstraintContainingConstructedGenericTypeReportsDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
    where T : Data<System.InvalidOperationException>
{
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void TupleElementTypeReportsDiagnosticAtOffendingTypeArgument()
    {
        const string offendingText = "System.InvalidOperationException";
        var source = """
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private (Data<System.InvalidOperationException> Item, int Count) value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Equal(offendingText, GetSourceText(source, diagnostic));
    }

    [Fact]
    public void CollectionExpressionReportsExplicitTargetTypeOnly()
    {
        const string explicitTypeArgument = "System.InvalidOperationException";
        var source = """
using System.Collections;
using System.Collections.Generic;
using IvTem.TypeSafety;

internal sealed class Bag<[DisallowTypes(typeof(System.Exception))] T> : IEnumerable<T>
{
    public void Add(T item)
    {
    }

    public IEnumerator<T> GetEnumerator()
        => throw null!;

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

internal sealed class Consumer
{
    private void Use()
    {
        Accept([]);
    }

    private static void Accept(Bag<System.InvalidOperationException> values)
    {
    }
}
""";

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001")
            .ToArray();

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(explicitTypeArgument, GetSourceText(source, diagnostic));
    }

    [Fact]
    public void AliasDeclarationAndAliasUseRemainSilent()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;
using ForbiddenData = Data<System.InvalidOperationException>;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private ForbiddenData? value;
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void BrokenConstructedGenericTypeUseRemainsSilent()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<MissingType>? value;
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    private static string GetSourceText(string source, Microsoft.CodeAnalysis.Diagnostic diagnostic)
        => source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
}
