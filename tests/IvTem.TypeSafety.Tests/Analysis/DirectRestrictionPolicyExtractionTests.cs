using System;
using System.Collections.Generic;
using System.Linq;
using IvTem.TypeSafety.Policies;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis;

public sealed class DirectRestrictionPolicyExtractionTests
{
    [Fact]
    public void ValidDirectDisallowTypesConfigurationProducesNoConfigurationDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<[DisallowTypes(typeof(System.Exception))] T>
{
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ValidDirectDisallowExactTypesConfigurationProducesNoConfigurationDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<[DisallowExactTypes(typeof(string))] T>
{
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void MultipleAttributesAndConstructorArgumentsAreAccumulated()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<
    [DisallowTypes(typeof(System.Exception), typeof(System.IO.Stream))]
    [DisallowTypes(typeof(System.IDisposable))]
    [DisallowExactTypes(typeof(string), typeof(int))]
    [DisallowExactTypes(typeof(long))]
    T>
{
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void DuplicateConfiguredTypesAreDeduplicatedWithoutDiagnostics()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<
    [DisallowTypes(typeof(System.Exception), typeof(System.Exception))]
    [DisallowTypes(typeof(System.Exception))]
    [DisallowExactTypes(typeof(string), typeof(string))]
    T>
{
}
""");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void DirectExtractorReturnsDeterministicDeduplicatedPolicy()
    {
        var compilation = AnalyzerTestHost.CreateGeneratedCompilation("""
using IvTem.TypeSafety;

namespace Consumer;

internal sealed class Sample<
    [DisallowTypes(typeof(System.Exception), typeof(System.IO.Stream), typeof(System.Exception))]
    [DisallowExactTypes(typeof(string), typeof(int), typeof(string))]
    T>
{
}
""");
        var typeParameter = compilation.GetTypeByMetadataName("Consumer.Sample`1")!.TypeParameters.Single();
        var diagnostics = new List<Diagnostic>();
        var extractor = new DirectRestrictionPolicyExtractor();

        var policy = extractor.Extract(typeParameter, diagnostics.Add, default);

        Assert.Empty(diagnostics);
        Assert.Same(typeParameter, policy.TypeParameter);
        Assert.Equal(new[] { "System.Exception", "System.IO.Stream" }, policy.DisallowAssignable.Select(type => type.DisplayName));
        Assert.Equal(new[] { "System.String", "System.Int32" }, policy.DisallowExact.Select(type => type.DisplayName));
    }

    [Theory]
    [InlineData("[DisallowTypes()]", "the type list is empty")]
    [InlineData("[DisallowExactTypes()]", "the type list is empty")]
    [InlineData("[DisallowTypes(null)]", "the type list is null")]
    [InlineData("[DisallowTypes(typeof(string), null)]", "the type list contains a null entry")]
    [InlineData("[DisallowTypes(typeof(System.Collections.Generic.IEnumerable<>))]", "open or unbound generic type")]
    [InlineData("[DisallowTypes(typeof(object))]", "DisallowTypes cannot be configured with System.Object")]
    public void InvalidDirectConfigurationReportsConfigurationDiagnostic(string attributeSource, string expectedReason)
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics($$"""
using IvTem.TypeSafety;

internal sealed class Sample<{{attributeSource}} T>
{
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS002", diagnostic.Id);
        Assert.Contains(expectedReason, diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ForbiddenTypeContainingSurroundingGenericParameterReportsMetadataDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<TOuter, [DisallowTypes(typeof(System.Collections.Generic.IEnumerable<TOuter>))] T>
{
}
""");

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS005", diagnostic.Id);
    }

    [Fact]
    public void MalformedLookalikeAttributeReportsMetadataDiagnostic()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
namespace IvTem.TypeSafety;

public sealed class DisallowTypesAttribute : System.Attribute
{
    public DisallowTypesAttribute(string value)
    {
    }
}

internal sealed class Sample<[IvTem.TypeSafety.DisallowTypes("bad")] T>
{
}
""", runGenerator: false);

        var diagnostic = Assert.Single(diagnostics);

        Assert.Equal("IVTS005", diagnostic.Id);
        Assert.Contains("does not match the expected v1 contract", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void DisallowExactTypesAllowsObjectConfiguration()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;

internal sealed class Sample<[DisallowExactTypes(typeof(object))] T>
{
}
""");

        Assert.Empty(diagnostics);
    }
}
