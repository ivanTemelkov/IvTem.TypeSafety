using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.Cycles;

public sealed class CycleDetectionTests
{
    [Fact]
    public void SimpleTwoTypeCycleReportsOneConfigurationDiagnostic()
    {
        const string source = """
using IvTem.TypeSafety;

internal sealed class A<T>
{
    private B<T>? value;
}

internal sealed class B<T>
{
    private A<T>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS003"));

        Assert.Equal("T", GetSourceText(source, diagnostic));
        Assert.Contains("A<T>.T", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("B<T>.T", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void LongerCycleReportsOneDeterministicConfigurationDiagnostic()
    {
        const string source = """
using IvTem.TypeSafety;

internal sealed class A<T>
{
    private B<T>? value;
}

internal sealed class B<T>
{
    private C<T>? value;
}

internal sealed class C<T>
{
    private A<T>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS003"));

        Assert.Equal("T", GetSourceText(source, diagnostic));
        Assert.Contains("A<T>.T, B<T>.T, C<T>.T", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateCyclePathsReportOneConfigurationDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class A<T>
{
    private B<T>? first;
    private C<T>? second;
}

internal sealed class B<T>
{
    private A<T>? value;
}

internal sealed class C<T>
{
    private A<T>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void OrdinaryNongenericRecursiveShapeDoesNotReportCycle()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Node
{
    private Node? next;
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void ConstructorOnGenericResultTypeDoesNotReportSelfCycle()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System.Diagnostics.CodeAnalysis;
using IvTem.TypeSafety;

public static class Result
{
    public static Result<T> Success<[DisallowTypes(typeof(System.Exception))] T>([NotNull] T data) => new Result<T>(data);
}

public sealed class Result<T>
{
    public T Data { get; set; }

    internal Result([NotNull] T data)
    {
        Data = data;
    }
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void SelfReturningGenericResultMethodsDoNotReportSelfCycle()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System.Diagnostics.CodeAnalysis;
using IvTem.TypeSafety;

namespace Demo.Results;

public static class Outcome
{
    public static Outcome<T> Failed<[DisallowTypes(typeof(System.Exception))] T>([NotNull] System.Exception error) where T : notnull
    {
        System.ArgumentNullException.ThrowIfNull(error);
        return Outcome<T>.Failed(error);
    }

    public static Outcome<T> Success<[DisallowTypes(typeof(System.Exception))] T>([NotNull] T value) where T : notnull
        => new(value);
}

public sealed class Outcome<T> where T : notnull
{
    private System.Exception? Error { get; init; }

    private T? Value { get; }

    private Outcome()
    {
    }

    public Outcome([NotNull] T value)
        => Value = value;

    internal static Outcome<T> Failed([NotNull] System.Exception error)
        => new()
        {
            Error = error
        };

    public bool TryGetValue([NotNullWhen(returnValue: true)] out T? value)
    {
        value = default;

        if (Error is not null)
            return false;

        if (Value is null)
            return false;

        value = Value;
        return true;
    }
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void CycleDoesNotOverflowAnalyzer()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class A<T>
{
    private B<T>? value;
}

internal sealed class B<T>
{
    private C<T>? value;
}

internal sealed class C<T>
{
    private D<T>? value;
}

internal sealed class D<T>
{
    private A<T>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void AcyclicDeepPropagationRemainsValid()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Root<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Middle<T>
{
    private Root<T>? value;
}

internal sealed class Leaf<T>
{
    private Middle<T>? value;
}

internal sealed class UseSite
{
    private Leaf<System.InvalidOperationException>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
    }

    [Fact]
    public void UseSiteDiagnosticDependingOnCyclicPropagationIsSuppressed()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class A<[DisallowTypes(typeof(System.Exception))] T>
{
    private B<T>? value;
}

internal sealed class B<T>
{
    private A<T>? value;
}

internal sealed class UseSite
{
    private B<System.InvalidOperationException>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS003"));
        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    private static string GetSourceText(string source, Microsoft.CodeAnalysis.Diagnostic diagnostic)
        => source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
}
