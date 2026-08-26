using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace IvTem.TypeSafety.Tests.Packaging;

[Collection("Package build")]
public sealed class PackageContentTests
{
    private const string PackageId = "IvTem.TypeSafety";
    private const string PackageVersion = "0.1.1";

    [Fact]
    public void PackageContainsAnalyzerAssetsOnly()
    {
        PackageArtifacts artifacts = PackProject();

        IReadOnlyCollection<string> packageEntries = ReadEntryNames(artifacts.PackagePath);
        IReadOnlyCollection<string> symbolEntries = ReadEntryNames(artifacts.SymbolPackagePath);
        string sourceLinkContent = Encoding.UTF8.GetString(
            ReadArchiveEntryBytes(
                artifacts.SymbolPackagePath,
                "analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.pdb"));
        XDocument nuspec = ReadNuspec(artifacts.PackagePath);

        Assert.DoesNotContain(packageEntries, entry => entry.StartsWith("lib/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(symbolEntries, entry => entry.StartsWith("lib/", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.dll", packageEntries);
        Assert.Contains("buildTransitive/IvTem.TypeSafety.props", packageEntries);
        Assert.Contains("README.md", packageEntries);
        Assert.Contains("analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.pdb", symbolEntries);
        AssertSymbolPackagePdbsMatchPackageDlls(packageEntries, symbolEntries);
        Assert.Contains("raw.githubusercontent.com", sourceLinkContent, StringComparison.Ordinal);

        Assert.Equal(PackageId, ReadMetadataValue(nuspec, "id"));
        Assert.Equal(PackageVersion, ReadMetadataValue(nuspec, "version"));
        Assert.Equal("Ivan Temelkov", ReadMetadataValue(nuspec, "authors"));
        Assert.Equal("README.md", ReadMetadataValue(nuspec, "readme"));

        XElement license = ReadMetadataElement(nuspec, "license");
        Assert.Equal("expression", license.Attribute("type")?.Value);
        Assert.Equal("MIT", license.Value);

        XElement repository = ReadMetadataElement(nuspec, "repository");
        Assert.Equal("git", repository.Attribute("type")?.Value);
        Assert.Equal("https://github.com/ivanTemelkov/IvTem.TypeSafety", repository.Attribute("url")?.Value);
        Assert.False(string.IsNullOrWhiteSpace(repository.Attribute("commit")?.Value));
    }

    private static PackageArtifacts PackProject()
    {
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot, "src", "IvTem.TypeSafety", "IvTem.TypeSafety.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-restore");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet pack.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(
            process.ExitCode == 0,
            $"dotnet pack failed with exit code {process.ExitCode}.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");

        string packageDirectory = Path.Combine(repositoryRoot, "src", "IvTem.TypeSafety", "bin", "Release");
        string packagePath = Path.Combine(packageDirectory, $"{PackageId}.{PackageVersion}.nupkg");
        string symbolPackagePath = Path.Combine(packageDirectory, $"{PackageId}.{PackageVersion}.snupkg");

        Assert.True(File.Exists(packagePath), $"Expected package was not created: {packagePath}");
        Assert.True(File.Exists(symbolPackagePath), $"Expected symbol package was not created: {symbolPackagePath}");

        return new PackageArtifacts(packagePath, symbolPackagePath);
    }

    private static IReadOnlyCollection<string> ReadEntryNames(string archivePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);

        return archive.Entries
            .Select(entry => entry.FullName)
            .ToArray();
    }

    private static void AssertSymbolPackagePdbsMatchPackageDlls(
        IReadOnlyCollection<string> packageEntries,
        IReadOnlyCollection<string> symbolEntries)
    {
        var packageDllEntries = new HashSet<string>(
            packageEntries.Where(entry => entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);

        foreach (string symbolEntry in symbolEntries.Where(entry => entry.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)))
        {
            string expectedDllEntry = Path.ChangeExtension(symbolEntry, ".dll").Replace('\\', '/');

            Assert.Contains(expectedDllEntry, packageDllEntries);
        }
    }

    private static XDocument ReadNuspec(string packagePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        ZipArchiveEntry entry = archive.GetEntry($"{PackageId}.nuspec")
            ?? throw new InvalidOperationException("The package does not contain a nuspec file.");

        using Stream stream = entry.Open();

        return XDocument.Load(stream);
    }

    private static byte[] ReadArchiveEntryBytes(string archivePath, string entryName)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry entry = archive.GetEntry(entryName)
            ?? throw new InvalidOperationException($"The archive does not contain '{entryName}'.");

        using Stream stream = entry.Open();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return memoryStream.ToArray();
    }

    private static string ReadMetadataValue(XDocument document, string localName)
    {
        return ReadMetadataElement(document, localName).Value;
    }

    private static XElement ReadMetadataElement(XDocument document, string localName)
    {
        return document.Descendants()
            .Single(element => string.Equals(element.Name.LocalName, localName, StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IvTem.TypeSafety.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }

    private sealed class PackageArtifacts
    {
        public PackageArtifacts(string packagePath, string symbolPackagePath)
        {
            PackagePath = packagePath;
            SymbolPackagePath = symbolPackagePath;
        }

        public string PackagePath { get; }

        public string SymbolPackagePath { get; }
    }
}
