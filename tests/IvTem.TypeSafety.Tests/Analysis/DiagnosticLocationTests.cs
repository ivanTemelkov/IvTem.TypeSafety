using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis;

public sealed class DiagnosticLocationTests
{
    [Fact]
    public void ExplicitGenericTypeDiagnosticUsesSourceBackedTypeArgumentLocation()
    {
        const string source = """
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        AssertSourceLocation(diagnostic, "System.InvalidOperationException", source);
    }

    [Fact]
    public void InferredGenericMethodDiagnosticUsesSourceBackedInvocationLocation()
    {
        const string source = """
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
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        AssertSourceLocation(diagnostic, "Create(value)", source);
    }

    [Fact]
    public void CrossAssemblyUseSiteDiagnosticUsesConsumerSourceLocation()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}
""", "ReferencedContracts");

        const string source = """
using Contracts;

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(
                source,
                additionalReferences: new[] { reference })
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        AssertSourceLocation(diagnostic, "System.InvalidOperationException", source);
    }

    [Fact]
    public void UseSiteDiagnosticIncludesSourceBackedRestrictionLocationWhenAvailable()
    {
        const string source = """
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS001"));

        var additionalLocation = Assert.Single(diagnostic.AdditionalLocations);
        Assert.True(additionalLocation.IsInSource);
        Assert.Equal(AnalyzerTestHost.DefaultSourcePath, additionalLocation.GetLineSpan().Path);
        Assert.Equal("DisallowTypes(typeof(System.Exception))", GetSourceText(source, additionalLocation));
    }

    [Fact]
    public void CycleDiagnosticUsesSourceBackedTypeParameterLocation()
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

        AssertSourceLocation(diagnostic, "T", source);
    }

    private static void AssertSourceLocation(Microsoft.CodeAnalysis.Diagnostic diagnostic, string expectedText, string source)
    {
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(AnalyzerTestHost.DefaultSourcePath, diagnostic.Location.GetLineSpan().Path);
        Assert.Equal(expectedText, GetSourceText(source, diagnostic.Location));
    }

    private static string GetSourceText(string source, Microsoft.CodeAnalysis.Location location)
        => source.Substring(location.SourceSpan.Start, location.SourceSpan.Length);
}
