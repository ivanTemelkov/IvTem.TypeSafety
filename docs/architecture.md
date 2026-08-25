# Architecture

`IvTem.TypeSafety` ships one Roslyn analyzer/source-generator assembly as NuGet analyzer assets.

## Generator and attributes

The source generator emits internal attributes into each consuming C# compilation:

- `IvTem.TypeSafety.DisallowTypesAttribute`
- `IvTem.TypeSafety.DisallowExactTypesAttribute`

The package intentionally has no runtime `lib/` assembly. Consuming assemblies record the generated attributes in their own metadata.

## Analyzer enforcement

The analyzer reads restriction attributes by fully qualified metadata name and expected constructor shape, not by shared CLR assembly identity. This lets enforcement work across source, project references, and compiled package references as long as the analyzer is active in the final consuming project.

The analyzer reports `IVTS001` when a constructed generic type or generic method uses a forbidden type argument. Additional diagnostics cover malformed restrictions and unsupported cyclic propagation.

## NuGet and MSBuild flow

The package contains:

- `analyzers/dotnet/cs/IvTem.TypeSafety.dll`
- `buildTransitive/IvTem.TypeSafety.props`

NuGet activates analyzer assets for direct package references. The `buildTransitive` props file also adds the same analyzer assembly for downstream projects that receive `IvTem.TypeSafety` transitively through normal package dependency flow.

The integration tests prove enforcement for:

- direct `PackageReference` to `IvTem.TypeSafety`;
- app project referencing a library project that normally references `IvTem.TypeSafety`;
- app package referencing a library package that normally depends on `IvTem.TypeSafety`.

Transparent enforcement still requires the final project build to receive and load the analyzer. If an intermediate project or package hides the dependency or excludes `buildTransitive`/analyzer assets, downstream enforcement is not claimed.
