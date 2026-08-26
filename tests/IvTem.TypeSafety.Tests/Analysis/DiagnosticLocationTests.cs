using System.IO;
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
        Assert.True(File.Exists(additionalLocation.GetLineSpan().Path));
        Assert.Equal(AnalyzerTestHost.DefaultSourcePath, Path.GetFileName(additionalLocation.GetLineSpan().Path));
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

    [Fact]
    public void InvalidConfigurationDiagnosticUsesSourceBackedAttributeLocation()
    {
        const string source = """
using IvTem.TypeSafety;

internal sealed class Sample<[DisallowTypes()] T>
{
}
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "IVTS002"));

        AssertSourceLocation(diagnostic, "DisallowTypes()", source);
    }

    [Fact]
    public void MalformedAttributeDiagnosticUsesSourceBackedAttributeLocation()
    {
        const string source = """
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
""";

        var diagnostic = Assert.Single(AnalyzerTestHost.GetAnalyzerDiagnostics(source, runGenerator: false)
            .Where(diagnostic => diagnostic.Id == "IVTS005"));

        AssertSourceLocation(diagnostic, "IvTem.TypeSafety.DisallowTypes(\"bad\")", source);
    }

    [Fact]
    public void SourceWithoutPhysicalFileIsIgnored()
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
""", sourcePath: "MissingSource.cs", writePhysicalSource: false);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GeneratedPhysicalSourceFileIsIgnored()
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
""", sourcePath: Path.Combine("obj", "Debug", "net10.0", "Generated.g.cs"));

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void RestrictionDeclaredInGeneratedPhysicalSourceFileIsIgnoredForPhysicalConsumerUseSite()
    {
        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics(new[]
        {
            new AnalyzerTestSource(
                """
using IvTem.TypeSafety;

internal sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}
""",
                Path.Combine("obj", "Debug", "net10.0", "Generated.g.cs")),
            new AnalyzerTestSource(
                """
internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""")
        });

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));
    }

    private static void AssertSourceLocation(Microsoft.CodeAnalysis.Diagnostic diagnostic, string expectedText, string source)
    {
        Assert.True(diagnostic.Location.IsInSource);
        Assert.True(File.Exists(diagnostic.Location.GetLineSpan().Path));
        Assert.Equal(AnalyzerTestHost.DefaultSourcePath, Path.GetFileName(diagnostic.Location.GetLineSpan().Path));
        Assert.Equal(expectedText, GetSourceText(source, diagnostic.Location));
    }

    private static string GetSourceText(string source, Microsoft.CodeAnalysis.Location location)
        => source.Substring(location.SourceSpan.Start, location.SourceSpan.Length);
}
