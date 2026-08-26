using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using IvTem.TypeSafety.Analysis;
using IvTem.TypeSafety.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IvTem.TypeSafety.Tests.TestInfrastructure;

internal static class AnalyzerTestHost
{
    public const string DefaultSourcePath = "Source.cs";

    public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics(
        string source,
        bool runGenerator = true,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string sourcePath = DefaultSourcePath,
        bool writePhysicalSource = true)
    {
        var compilation = CreateCompilation(
            source,
            additionalReferences: additionalReferences,
            sourcePath: sourcePath,
            writePhysicalSource: writePhysicalSource);

        if (runGenerator)
            compilation = RunGenerator(compilation);

        var analyzer = new TypeSafetyAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult()
            .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    public static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics(
        IEnumerable<AnalyzerTestSource> sources,
        bool runGenerator = true,
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var compilation = CreateCompilation(sources, additionalReferences: additionalReferences);

        if (runGenerator)
            compilation = RunGenerator(compilation);

        var analyzer = new TypeSafetyAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult()
            .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToImmutableArray();
    }

    public static CSharpCompilation CreateGeneratedCompilation(string source)
        => RunGenerator(CreateCompilation(source));

    public static MetadataReference CreateGeneratedMetadataReference(string source, string assemblyName)
        => CreateMetadataReference(source, assemblyName, runGenerator: true);

    public static MetadataReference CreateMetadataReference(string source, string assemblyName, bool runGenerator = true)
    {
        using var stream = new MemoryStream();
        var compilation = CreateCompilation(source, assemblyName);
        if (runGenerator)
            compilation = RunGenerator(compilation);

        var emitResult = compilation.Emit(stream);

        if (emitResult.Success == false)
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics.Select(diagnostic => diagnostic.ToString())));

        stream.Position = 0;
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName = "Consumer",
        IEnumerable<MetadataReference>? additionalReferences = null,
        string sourcePath = DefaultSourcePath,
        bool writePhysicalSource = true)
        => CreateCompilation(
            new[] { new AnalyzerTestSource(source, sourcePath, writePhysicalSource) },
            assemblyName,
            additionalReferences);

    private static CSharpCompilation CreateCompilation(
        IEnumerable<AnalyzerTestSource> sources,
        string assemblyName = "Consumer",
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var syntaxTrees = sources
            .Select(source =>
                CSharpSyntaxTree.ParseText(
                    source.Source,
                    parseOptions,
                    PrepareSourcePath(source.SourcePath, source.Source, source.WritePhysicalSource)))
            .ToArray();
        var references = GetReferences()
            .Concat(additionalReferences ?? Enumerable.Empty<MetadataReference>())
            .ToArray();

        return CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static CSharpCompilation RunGenerator(CSharpCompilation compilation)
    {
        IIncrementalGenerator generator = new TypeSafetyAttributeGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees.First().Options);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var updatedCompilation,
            out var diagnostics);

        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        if (errors.Length > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(diagnostic => diagnostic.ToString())));

        return (CSharpCompilation)updatedCompilation;
    }

    private static IReadOnlyCollection<MetadataReference> GetReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrEmpty(trustedPlatformAssemblies))
            return new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
            };

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    private static string PrepareSourcePath(string sourcePath, string source, bool writePhysicalSource)
    {
        if (writePhysicalSource == false)
            return sourcePath;

        var physicalSourcePath = Path.IsPathRooted(sourcePath)
            ? sourcePath
            : Path.Combine(Path.GetTempPath(), "IvTem.TypeSafety.Tests", Guid.NewGuid().ToString("N"), sourcePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalSourcePath) ?? Path.GetTempPath());
        File.WriteAllText(physicalSourcePath, source);

        return physicalSourcePath;
    }
}

internal sealed class AnalyzerTestSource
{
    public AnalyzerTestSource(string source, string sourcePath = AnalyzerTestHost.DefaultSourcePath, bool writePhysicalSource = true)
    {
        Source = source;
        SourcePath = sourcePath;
        WritePhysicalSource = writePhysicalSource;
    }

    public string Source { get; }

    public string SourcePath { get; }

    public bool WritePhysicalSource { get; }
}
