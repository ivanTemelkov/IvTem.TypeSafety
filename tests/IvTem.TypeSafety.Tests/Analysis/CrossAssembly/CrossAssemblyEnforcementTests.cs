using System;
using System.Linq;
using IvTem.TypeSafety.Tests.TestInfrastructure;
using Xunit;

namespace IvTem.TypeSafety.Tests.Analysis.CrossAssembly;

public sealed class CrossAssemblyEnforcementTests
{
    [Fact]
    public void ReferencedGenericTypeContractReportsUseSiteDiagnostic()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public sealed class Data<[DisallowTypes(typeof(System.Exception))] T>
{
}
""", "ReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using Contracts;

internal sealed class Consumer
{
    private Data<System.InvalidOperationException>? value;
}
""", additionalReferences: new[] { reference });

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("System.InvalidOperationException", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void EmbeddedAttributeAssemblyIdentityIsIgnored()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public sealed class Data<[DisallowExactTypes(typeof(string))] T>
{
}
""", "ReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using IvTem.TypeSafety;
using Contracts;

internal sealed class Local<[DisallowExactTypes(typeof(int))] T>
{
}

internal sealed class Consumer
{
    private Data<string>? referencedValue;
    private Local<int>? localValue;
}
""", additionalReferences: new[] { reference });

        var forbiddenDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.Id == "IVTS001")
            .ToArray();

        Assert.Equal(2, forbiddenDiagnostics.Length);
        Assert.Contains(forbiddenDiagnostics, diagnostic => diagnostic.GetMessage().Contains("System.String", StringComparison.Ordinal));
        Assert.Contains(forbiddenDiagnostics, diagnostic => diagnostic.GetMessage().Contains("System.Int32", StringComparison.Ordinal));
    }

    [Fact]
    public void ReferencedGenericMethodContractReportsUseSiteDiagnostic()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public static class Factory
{
    public static T Create<[DisallowTypes(typeof(System.Exception))] T>(T value)
        => value;
}
""", "ReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using Contracts;

internal sealed class Consumer
{
    private void Use(System.InvalidOperationException value)
    {
        _ = Factory.Create(value);
    }
}
""", additionalReferences: new[] { reference });

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("System.InvalidOperationException", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ReferencedPropagatedTypeContractReportsUseSiteDiagnostic()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public class Base<[DisallowTypes(typeof(System.Exception))] T>
{
}

public sealed class Derived<T> : Base<T>
{
}
""", "ReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using Contracts;

internal sealed class Consumer
{
    private Derived<System.ApplicationException>? value;
}
""", additionalReferences: new[] { reference });

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("System.ApplicationException", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.Exception)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void MetadataOnlyMalformedLookalikeIsIgnoredDefensively()
    {
        var reference = AnalyzerTestHost.CreateMetadataReference("""
namespace IvTem.TypeSafety
{
    public sealed class DisallowTypesAttribute : System.Attribute
    {
        public DisallowTypesAttribute(string value)
        {
        }
    }
}

namespace Contracts
{
    public sealed class Data<[IvTem.TypeSafety.DisallowTypes("bad")] T>
    {
    }
}
""", "MalformedReferencedContracts", runGenerator: false);

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using Contracts;

internal sealed class Consumer
{
    private Data<System.Exception>? value;
}
""", additionalReferences: new[] { reference });

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id is "IVTS001" or "IVTS005"));
    }

    [Fact]
    public void MetadataOnlyInvalidConfigurationDoesNotRequireSourceLocation()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace Contracts;

public sealed class Data<[DisallowTypes()] T>
{
}
""", "InvalidReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using Contracts;

internal sealed class Consumer
{
    private Data<System.Exception>? value;
}
""", additionalReferences: new[] { reference });

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Id is "IVTS001" or "IVTS002"));
    }

    [Fact]
    public void GeneratedDeclarationInReferencedAssemblyIsEnforcedFromMetadata()
    {
        var reference = AnalyzerTestHost.CreateGeneratedMetadataReference("""
using IvTem.TypeSafety;

namespace GeneratedContracts;

public sealed class GeneratedData<[DisallowTypes(typeof(System.IDisposable))] T>
{
}
""", "GeneratedReferencedContracts");

        var diagnostics = AnalyzerTestHost.GetAnalyzerDiagnostics("""
using GeneratedContracts;

internal sealed class DisposableValue : System.IDisposable
{
    public void Dispose()
    {
    }
}

internal sealed class Consumer
{
    private GeneratedData<DisposableValue>? value;
}
""", additionalReferences: new[] { reference });

        var diagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "IVTS001"));

        Assert.Contains("DisposableValue", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("DisallowTypes(System.IDisposable)", diagnostic.GetMessage(), StringComparison.Ordinal);
    }
}
