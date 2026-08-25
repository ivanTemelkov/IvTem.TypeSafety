using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.SignaturePropagation;

public sealed class SignaturePropagationTests
{
    [Fact]
    public void FieldSignaturePropagatesContract()
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

internal sealed class UseSite
{
    private Consumer<System.InvalidOperationException>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("System.InvalidOperationException", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void PropertySignaturePropagatesContract()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
{
    public Data<T>? Value { get; set; }
}

internal sealed class UseSite
{
    private Consumer<System.Exception>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("DisallowExactTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MethodReturnAndParameterSignaturesPropagateContracts()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Input<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Output<[DisallowExactTypes(typeof(System.InvalidOperationException))] T>
{
}

internal sealed class Consumer<T>
{
    public Output<T>? Convert(Input<T>? value) => null;
}

internal sealed class UseSite
{
    private Consumer<System.InvalidOperationException>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowExactTypes(System.InvalidOperationException)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void EventSignaturePropagatesContractThroughNestedContainer()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System;
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
{
    public event Action<Data<T>>? Changed;
}

internal sealed class UseSite
{
    private Consumer<System.ApplicationException>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void PrivateStaticMembersParticipateInSignaturePropagation()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
{
    private static Data<T>? value;
}

internal sealed class UseSite
{
    private Consumer<System.Exception>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void GenericConstraintSignaturePropagatesContract()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T, TData>
    where TData : Data<T>
{
}

internal sealed class Derived : Data<System.InvalidOperationException>
{
}

internal sealed class UseSite
{
    private Consumer<System.InvalidOperationException, Derived>? value;
}
""");

        var diagnostic = Assert.Single(
            diagnostics,
            diagnostic => diagnostic.Id == "IVTS001"
                && diagnostic.GetMessage().Contains("Consumer<T, TData>", StringComparison.Ordinal));

        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MethodBodyLocalDoesNotPropagateContract()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T>
{
    public void Execute()
    {
        Data<T>? value = null;
    }
}

internal sealed class UseSite
{
    private Consumer<System.Exception>? value;
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void TransformedTypeArgumentsDoNotPropagateContract()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using System.Collections.Generic;
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Wrapper<T>
{
}

internal sealed class Consumer<T>
{
    private Data<List<T>>? list;
    private Data<T[]>? array;
    private Data<(T, string)>? tuple;
    private Data<Wrapper<T>>? wrapper;
}

internal sealed class UseSite
{
    private Consumer<System.Exception>? value;
}
""");

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void NestedTypeOwnParameterPropagatesButContainingParameterDoesNot()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Outer<TOuter>
{
    internal sealed class Inner<TInner>
    {
        private Data<TOuter>? outerValue;
        private Data<TInner>? innerValue;
    }
}

internal sealed class UseSite
{
    private Outer<string>.Inner<System.Exception>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }
}
