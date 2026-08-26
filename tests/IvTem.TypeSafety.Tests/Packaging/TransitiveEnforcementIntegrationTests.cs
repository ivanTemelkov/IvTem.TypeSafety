using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace IvTem.TypeSafety.Tests.Packaging;

[Collection("Package build")]
public sealed class TransitiveEnforcementIntegrationTests
{
    private const string PackageId = "IvTem.TypeSafety";
    private const string PackageVersion = "0.1.5";

    [Fact]
    public void DirectPackageReferenceEnforcesRestrictions()
    {
        using var workspace = IntegrationWorkspace.Create();
        workspace.AddTypeSafetyPackage();

        string projectPath = workspace.WriteProject(
            "Consumer",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="IvTem.TypeSafety" Version="0.1.5" />
              </ItemGroup>
            </Project>
            """,
            """
            using IvTem.TypeSafety;

            namespace Consumer;

            public sealed class Restricted<[DisallowTypes(typeof(string))] T>
            {
            }

            public sealed class UseSite
            {
                private Restricted<string>? Value { get; set; }
            }
            """);

        ProcessResult result = DotNet("build", projectPath, "--nologo", "-v:minimal")
            .Run(workspace.RootPath);

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains("IVTS001", result.Output, StringComparison.Ordinal);
        AssertDiagnosticOutputIncludesSourceFile(result.Output);
    }

    [Fact]
    public void ProjectReferenceFlowsBuildTransitiveAnalyzerToConsumerProject()
    {
        using var workspace = IntegrationWorkspace.Create();
        workspace.AddTypeSafetyPackage();
        string libraryProjectPath = workspace.WriteLibraryProject();

        string projectPath = workspace.WriteProject(
            "Consumer",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <ProjectReference Include="..\LibraryA\LibraryA.csproj" />
              </ItemGroup>
            </Project>
            """,
            """
            using LibraryA;

            namespace Consumer;

            public sealed class UseSite
            {
                private Restricted<string>? Value { get; set; }
            }
            """);

        ProcessResult libraryResult = DotNet("build", libraryProjectPath, "--nologo", "-v:minimal")
            .Run(workspace.RootPath);
        Assert.True(libraryResult.Succeeded, libraryResult.Output);

        ProcessResult result = DotNet("build", projectPath, "--nologo", "-v:minimal")
            .Run(workspace.RootPath);

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains("IVTS001", result.Output, StringComparison.Ordinal);
        AssertDiagnosticOutputIncludesSourceFile(result.Output);
    }

    [Fact]
    public void TransitivePackageDependencyEnforcesRestrictionsThroughBuildTransitiveAsset()
    {
        using var workspace = IntegrationWorkspace.Create();
        workspace.AddTypeSafetyPackage();

        string libraryProjectPath = workspace.WriteLibraryProject();
        ProcessResult packResult = DotNet("pack", libraryProjectPath, "-c", "Release", "-o", workspace.PackageSourcePath, "--nologo", "-v:minimal")
            .Run(workspace.RootPath);
        Assert.True(packResult.Succeeded, packResult.Output);

        string projectPath = workspace.WriteProject(
            "Consumer",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>disable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="LibraryA" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """,
            """
            using LibraryA;

            namespace Consumer;

            public sealed class UseSite
            {
                private Restricted<string>? Value { get; set; }
            }
            """);

        ProcessResult result = DotNet("build", projectPath, "--nologo", "-v:minimal")
            .Run(workspace.RootPath);

        Assert.False(result.Succeeded, result.Output);
        Assert.Contains("IVTS001", result.Output, StringComparison.Ordinal);
        AssertDiagnosticOutputIncludesSourceFile(result.Output);
    }

    private static void AssertDiagnosticOutputIncludesSourceFile(string output)
        => Assert.Contains(Path.Combine("Consumer", "Source.cs"), output, StringComparison.Ordinal);

    private static DotNetCommand DotNet(params string[] arguments)
        => new(arguments);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "IvTem.TypeSafety.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find repository root.");
    }

    private sealed class IntegrationWorkspace : IDisposable
    {
        private IntegrationWorkspace(string rootPath)
        {
            RootPath = rootPath;
            PackageSourcePath = Path.Combine(rootPath, "packages");
            Directory.CreateDirectory(PackageSourcePath);
            WriteNuGetConfig();
        }

        public string RootPath { get; }

        public string PackageSourcePath { get; }

        public static IntegrationWorkspace Create()
        {
            string rootPath = Path.Combine(Path.GetTempPath(), "IvTem.TypeSafety.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootPath);

            return new IntegrationWorkspace(rootPath);
        }

        public void AddTypeSafetyPackage()
        {
            string packagePath = PackTypeSafetyPackage();
            File.Copy(
                packagePath,
                Path.Combine(PackageSourcePath, Path.GetFileName(packagePath)),
                overwrite: true);
        }

        public string WriteLibraryProject()
            => WriteProject(
                "LibraryA",
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <ImplicitUsings>disable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <PackageId>LibraryA</PackageId>
                    <Version>1.0.0</Version>
                  </PropertyGroup>

                  <ItemGroup>
                    <PackageReference Include="IvTem.TypeSafety" Version="0.1.5" />
                  </ItemGroup>
                </Project>
                """,
                """
                using IvTem.TypeSafety;

                namespace LibraryA;

                public sealed class Restricted<[DisallowTypes(typeof(string))] T>
                {
                }
                """);

        public string WriteProject(string projectName, string projectFileContent, string sourceContent)
        {
            string projectDirectory = Path.Combine(RootPath, projectName);
            Directory.CreateDirectory(projectDirectory);

            string projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
            File.WriteAllText(projectPath, projectFileContent);
            File.WriteAllText(Path.Combine(projectDirectory, "Source.cs"), sourceContent);

            return projectPath;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(RootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void WriteNuGetConfig()
        {
            string configPath = Path.Combine(RootPath, "NuGet.config");
            File.WriteAllText(
                configPath,
                $$"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <config>
                    <add key="globalPackagesFolder" value="{{Path.Combine(RootPath, "global-packages")}}" />
                  </config>
                  <packageSources>
                    <clear />
                    <add key="local" value="{{PackageSourcePath}}" />
                  </packageSources>
                </configuration>
                """);
        }

        private static string PackTypeSafetyPackage()
        {
            string repositoryRoot = FindRepositoryRoot();
            string projectPath = Path.Combine(repositoryRoot, "src", "IvTem.TypeSafety", "IvTem.TypeSafety.csproj");

            ProcessResult result = DotNet("pack", projectPath, "-c", "Release", "--no-restore", "--nologo", "-v:minimal")
                .Run(repositoryRoot);

            if (result.Succeeded == false)
                throw new InvalidOperationException(result.Output);

            string packagePath = Path.Combine(
                repositoryRoot,
                "src",
                "IvTem.TypeSafety",
                "bin",
                "Release",
                $"{PackageId}.{PackageVersion}.nupkg");

            if (File.Exists(packagePath) == false)
                throw new FileNotFoundException("Expected package was not created.", packagePath);

            return packagePath;
        }
    }

    private sealed class DotNetCommand
    {
        private readonly string[] commandArguments;

        public DotNetCommand(string[] arguments)
        {
            commandArguments = arguments;
        }

        public ProcessResult Run(string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            foreach (string argument in commandArguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ProcessResult(process.ExitCode, standardOutput + standardError);
        }
    }

    private sealed class ProcessResult
    {
        public ProcessResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; }

        public string Output { get; }

        public bool Succeeded => ExitCode == 0;
    }
}
