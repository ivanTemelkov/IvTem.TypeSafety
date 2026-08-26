# Changelog

All notable changes to `IvTem.TypeSafety` will be documented in this file.

## 0.1.3

- Fixed false `IVTS003` self-cycle diagnostics for generic types whose own member signatures return the same constructed generic type, such as factory methods returning `Envelope<T>` from inside `Envelope<T>`.

## 0.1.2

- Lowered the Roslyn dependency baseline from `Microsoft.CodeAnalysis.CSharp` `5.6.0` to `4.8.0` so the analyzer can load in older .NET 8 development environments.

## 0.1.1

- Fixed the NuGet symbol package layout by placing the analyzer DLL under `analyzers/dotnet/cs/netstandard2.0/`, matching the `.snupkg` PDB path required by nuget.org validation.
- Updated the `buildTransitive` analyzer reference and package-content validation for the corrected analyzer asset path.

## 0.1.0

- Initial repository scaffolding.
- Embedded attribute source generator.
- Generic type-safety analyzer with direct, broad use-site, propagated, cross-assembly, and cycle-detection coverage.
- Analyzer-only NuGet package layout under `analyzers/dotnet/cs/` with no runtime `lib/` assembly.
- Automated package-content validation for NuGet metadata, analyzer assets, README inclusion, and symbol package output.
- `buildTransitive` package asset for tested downstream analyzer enforcement through normal project and package dependency flow.
- Temporary-project integration tests for direct package, project-reference, and package-transitive enforcement.
- User-facing README, architecture, diagnostics, limitations documentation, and runnable sample project.
- Stabilized solution-level Release packing by keeping the runnable sample non-packable.
