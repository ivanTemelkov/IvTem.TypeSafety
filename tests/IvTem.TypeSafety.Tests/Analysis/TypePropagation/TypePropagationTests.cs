using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.TypePropagation;

public sealed class TypePropagationTests
{
    [Fact]
    public void GenericInterfaceContractIsEnforcedOnImplementingType()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IContract<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer<T> : IContract<T>
{
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
    public void GenericBaseClassContractIsEnforcedOnDerivedType()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal class Base<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Derived<T> : Base<T>
{
}

internal sealed class UseSite
{
    private Derived<System.Exception>? value;
}
""");

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("DisallowExactTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleInheritedContractsAreUnioned()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IAssignable<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal interface IExact<[DisallowExactTypes(typeof(System.InvalidOperationException))] T>
{
}

internal sealed class Consumer<T> : IAssignable<T>, IExact<T>
{
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
    public void TransitiveInheritanceContractIsEnforced()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IRoot<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal class Middle<T> : IRoot<T>
{
}

internal sealed class Leaf<T> : Middle<T>
{
}

internal sealed class UseSite
{
    private Leaf<System.ApplicationException>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void PartialDeclarationContractsAreCombinedByOrdinal()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed partial class Consumer<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed partial class Consumer<[DisallowExactTypes(typeof(System.InvalidOperationException))] T>
{
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
    public void ReorderedGenericParameterMappingsAreApplied()
    {
        const string offendingText = "System.Exception";
        var source = """
using IvTem.TypeSafety;

internal interface IContract<TAllowed, [DisallowExactTypes(typeof(System.Exception))] TBlocked>
{
}

internal sealed class Consumer<TBlocked, TAllowed> : IContract<TAllowed, TBlocked>
{
}

internal sealed class UseSite
{
    private Consumer<System.Exception, string>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Equal(offendingText, GetSourceText(source, diagnostic));
        Assert.Contains("TBlocked", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedGenericParameterMappingsAreUnioned()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IContract<
    [DisallowTypes(typeof(System.Exception))] TAssignable,
    [DisallowExactTypes(typeof(System.InvalidOperationException))] TExact>
{
}

internal sealed class Consumer<T> : IContract<T, T>
{
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
    public void ConcreteForbiddenTypeInBaseDeclarationIsReportedImmediately()
    {
        const string offendingText = "System.Exception";
        var source = """
using IvTem.TypeSafety;

internal class Base<[DisallowExactTypes(typeof(System.Exception))] T>
{
}

internal sealed class Derived : Base<System.Exception>
{
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Equal(offendingText, GetSourceText(source, diagnostic));
    }

    [Fact]
    public void ConcreteForbiddenTypeInInterfaceDeclarationIsReportedImmediately()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IContract<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Derived : IContract<System.ApplicationException>
{
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    [Fact]
    public void DuplicateDiamondInterfacePathsReportOneDiagnosticPerOffendingGenericArgument()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal interface IRoot<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal interface ILeft<T> : IRoot<T>
{
}

internal interface IRight<T> : IRoot<T>
{
}

internal sealed class Consumer<T> : ILeft<T>, IRight<T>
{
}

internal sealed class UseSite
{
    private Consumer<System.Exception>? value;
}
""");

        Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    private static string GetSourceText(string source, Microsoft.CodeAnalysis.Diagnostic diagnostic)
        => source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
}
