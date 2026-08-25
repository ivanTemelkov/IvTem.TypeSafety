using System;
using System.Collections.Immutable;
using System.Linq;
using IvTem.TypeSafety.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace IvTem.TypeSafety.Tests.Generation;

public sealed class TypeSafetyAttributeGeneratorTests
{
    [Fact]
    public void GeneratorEmitsBothAttributeSources()
    {
        var result = RunGenerator("""
namespace Consumer;

internal sealed class Sample<T>
{
}
""");

        var generatedTree = GetGeneratedAttributeTree(result);
        var source = generatedTree.GetText().ToString();

        Assert.Contains("internal sealed class DisallowTypesAttribute", source, StringComparison.Ordinal);
        Assert.Contains("internal sealed class DisallowExactTypesAttribute", source, StringComparison.Ordinal);
        Assert.Contains("/// <summary>", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConsumerCanApplyAttributesToGenericParameters()
    {
        var result = RunGenerator("""
namespace Consumer;

using IvTem.TypeSafety;

internal sealed class Sample<
    [DisallowTypes(typeof(string))]
    [DisallowExactTypes(typeof(int))]
    T>
{
}
""");

        var compilation = result.Compilation;
        var diagnostics = GetErrors(compilation);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GeneratedAttributesAreInternal()
    {
        var result = RunGenerator("""
namespace Consumer;

internal sealed class Sample<T>
{
}
""");

        var compilation = result.Compilation;

        Assert.Equal(Accessibility.Internal, GetAttributeType(compilation, "DisallowTypesAttribute").DeclaredAccessibility);
        Assert.Equal(Accessibility.Internal, GetAttributeType(compilation, "DisallowExactTypesAttribute").DeclaredAccessibility);
    }

    [Fact]
    public void GeneratedAttributesAllowMultipleUsage()
    {
        var result = RunGenerator("""
namespace Consumer;

using IvTem.TypeSafety;

internal sealed class Sample<
    [DisallowTypes(typeof(string))]
    [DisallowTypes(typeof(object))]
    [DisallowExactTypes(typeof(int))]
    [DisallowExactTypes(typeof(long))]
    T>
{
}
""");

        var compilation = result.Compilation;
        var diagnostics = GetErrors(compilation);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void GeneratedAttributesHaveExpectedNamespaceAndConstructorShape()
    {
        var result = RunGenerator("""
namespace Consumer;

internal sealed class Sample<T>
{
}
""");

        var compilation = result.Compilation;

        AssertAttributeShape(GetAttributeType(compilation, "DisallowTypesAttribute"));
        AssertAttributeShape(GetAttributeType(compilation, "DisallowExactTypesAttribute"));
    }

    [Fact]
    public void ConsumerCompilationDoesNotReferenceAnalyzerAssembly()
    {
        var result = RunGenerator("""
namespace Consumer;

using IvTem.TypeSafety;

internal sealed class Sample<[DisallowTypes(typeof(string))] T>
{
}
""");

        var compilation = result.Compilation;
        var diagnostics = GetErrors(compilation);
        var hasAnalyzerAssemblyReference = compilation.References
            .Select(reference => compilation.GetAssemblyOrModuleSymbol(reference))
            .OfType<IAssemblySymbol>()
            .Any(assembly => assembly.Name.Equals("IvTem.TypeSafety", StringComparison.Ordinal));

        Assert.Empty(diagnostics);
        Assert.False(hasAnalyzerAssemblyReference);
    }

    private static GeneratorRunResult RunGenerator(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };
        var compilation = CSharpCompilation.Create(
            assemblyName: "Consumer",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator generator = new TypeSafetyAttributeGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: parseOptions);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics);

        Assert.Empty(diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        return new GeneratorRunResult(updatedCompilation);
    }

    private static SyntaxTree GetGeneratedAttributeTree(GeneratorRunResult result)
    {
        var generatedTree = result.Compilation.SyntaxTrees
            .SingleOrDefault(tree =>
            {
                var source = tree.GetText().ToString();

                return source.Contains("DisallowTypesAttribute", StringComparison.Ordinal)
                    && source.Contains("DisallowExactTypesAttribute", StringComparison.Ordinal);
            });

        Assert.NotNull(generatedTree);

        return generatedTree;
    }

    private static INamedTypeSymbol GetAttributeType(Compilation compilation, string metadataName)
    {
        var attributeType = compilation.GetTypeByMetadataName($"IvTem.TypeSafety.{metadataName}");

        Assert.NotNull(attributeType);

        return attributeType;
    }

    private static ImmutableArray<Diagnostic> GetErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

    private static void AssertAttributeShape(INamedTypeSymbol attributeType)
    {
        Assert.Equal("IvTem.TypeSafety", attributeType.ContainingNamespace.ToDisplayString());

        var constructor = attributeType.InstanceConstructors.Single();
        var parameter = constructor.Parameters.Single();

        Assert.True(parameter.IsParams);
        Assert.Equal("System.Type[]", parameter.Type.ToDisplayString());

        var attributeUsage = attributeType.GetAttributes()
            .Single(attribute => attribute.AttributeClass?.ToDisplayString() == "System.AttributeUsageAttribute");
        var constructorArgument = Assert.Single(attributeUsage.ConstructorArguments);

        Assert.Equal((int)AttributeTargets.GenericParameter, constructorArgument.Value);
        Assert.Contains(
            attributeUsage.NamedArguments,
            pair => pair.Key.Equals("AllowMultiple", StringComparison.Ordinal)
                && pair.Value.Value is true);
    }

    private sealed record GeneratorRunResult(Compilation Compilation);
}
