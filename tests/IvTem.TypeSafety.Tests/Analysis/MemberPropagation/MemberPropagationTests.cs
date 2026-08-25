using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.MemberPropagation;

public sealed class MemberPropagationTests
{
    [Fact]
    public void InterfaceGenericMethodContractIsEnforcedOnImplementationWithoutCopiedAttribute()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface ICreator
{
    void Create<[DisallowTypes(typeof(System.Exception))] T>();
}

internal sealed class Consumer : ICreator
{
    public void Use()
    {
        Create<System.Exception>();
    }

    public void Create<T>()
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
    public void OverrideGenericMethodContractIsEnforcedOnOverrideWithoutCopiedAttribute()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal abstract class BaseConsumer
{
    public abstract void Create<[DisallowTypes(typeof(System.Exception))] T>();
}

internal sealed class Consumer : BaseConsumer
{
    public void Use()
    {
        Create<System.InvalidOperationException>();
    }

    public override void Create<T>()
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
    public void ExplicitInterfaceImplementationContractIsEnforced()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface ICreator
{
    void Create<[DisallowExactTypes(typeof(System.Exception))] T>();
}

internal sealed class Consumer : ICreator
{
    public void Use()
    {
        ((ICreator)this).Create<System.Exception>();
    }

    void ICreator.Create<T>()
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("DisallowExactTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleInterfaceContractsAreUnioned()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IAssignableCreator
{
    void Create<[DisallowTypes(typeof(System.Exception))] T>();
}

internal interface IExactCreator
{
    void Create<[DisallowExactTypes(typeof(System.InvalidOperationException))] T>();
}

internal sealed class Consumer : IAssignableCreator, IExactCreator
{
    public void Use()
    {
        Create<System.InvalidOperationException>();
    }

    public void Create<T>()
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowExactTypes(System.InvalidOperationException)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void InheritedContractsMapMethodTypeParametersByOrdinal()
    {
        const string offendingText = "System.Exception";
        var source = """
using IvTem.TypeSafety;

internal interface ICreator
{
    void Create<TAllowed, [DisallowExactTypes(typeof(System.Exception))] TBlocked>();
}

internal sealed class Consumer : ICreator
{
    public void Use()
    {
        Create<string, System.Exception>();
    }

    public void Create<TAllowed, TBlocked>()
    {
    }
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source));
        var spanText = source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);

        Assert.Equal(offendingText, spanText);
        Assert.Contains("TBlocked", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void PartialMethodDeclarationContractIsEnforcedOnImplementationUse()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed partial class Consumer
{
    public void Use()
    {
        Create<System.Exception>();
    }

    partial void Create<[DisallowExactTypes(typeof(System.Exception))] T>();

    partial void Create<T>()
    {
    }
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS001", diagnostic.Id);
        Assert.Contains("DisallowExactTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateInterfacePathsReportOneDiagnosticPerOffendingGenericArgument()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IBaseCreator
{
    void Create<[DisallowTypes(typeof(System.Exception))] T>();
}

internal interface ILeftCreator : IBaseCreator
{
}

internal interface IRightCreator : IBaseCreator
{
}

internal sealed class Consumer : ILeftCreator, IRightCreator
{
    public void Use()
    {
        Create<System.Exception>();
    }

    public void Create<T>()
    {
    }
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }
}
