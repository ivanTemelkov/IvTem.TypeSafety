# Changelog

All notable changes to `IvTem.TypeSafety` will be documented in this file.

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
