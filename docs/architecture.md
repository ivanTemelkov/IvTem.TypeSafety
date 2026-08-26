# Architecture

`IvTem.TypeSafety` ships one Roslyn analyzer/source-generator assembly as NuGet analyzer assets. The package provides compile-time checks only; it intentionally has no runtime API surface.

## Generator and Analyzer Split

The incremental source generator emits internal attributes into each consuming C# compilation:

- `IvTem.TypeSafety.DisallowTypesAttribute`
- `IvTem.TypeSafety.DisallowExactTypesAttribute`

Both attributes target generic parameters, allow multiple applications, and accept `params System.Type[]`. Because the attributes are generated as internal source, consuming assemblies record their own copy of the metadata contract without referencing a runtime `IvTem.TypeSafety` assembly.

The diagnostic analyzer reads those attributes from source or metadata and enforces the stable v1 behavior. The generator owns attribute availability; the analyzer owns parsing, propagation, matching, and diagnostics.

## Policy Model

Each generic type parameter is represented as a restriction policy containing two independent sets:

- assignable restrictions from `DisallowTypes`;
- exact restrictions from `DisallowExactTypes`.

Forbidden types are de-duplicated by semantic symbol identity per restriction kind. Ordering remains deterministic by declaration order and display name so diagnostics stay stable.

Attribute recognition uses the fully qualified metadata names `IvTem.TypeSafety.DisallowTypesAttribute` and `IvTem.TypeSafety.DisallowExactTypesAttribute` plus the expected constructor shape. It does not require shared CLR identity with the analyzer assembly.

## Matching

`DisallowExactTypes` rejects only semantic identity after normalizing `dynamic` to `object` and erasing nullable reference annotations. Nullable value types remain distinct constructed types, so `int?` is not the same as `int`.

`DisallowTypes` rejects type arguments that are definitely assignable to a forbidden type through identity, reference, boxing, variance, array covariance, direct generic-parameter constraints, and implemented interface relationships. Numeric conversions and user-defined conversions are not treated as assignability.

The analyzer reports one `IVTS001` per offending generic argument, aggregating direct, inherited, and propagated matches into a single diagnostic message for that argument.

## Use Sites

The analyzer validates generic types and generic methods in the supported v1 use-site surface, including:

- fields, properties, parameters, return types, events, constraints, base and interface declarations;
- object creation, target-typed `new`, `typeof`, casts, `is`, `as`, `default(...)`, tuple element types, and collection expressions when Roslyn exposes a constructed target type;
- explicit generic method calls, inferred generic method calls, method groups, delegate conversions, and generic local functions.

Unresolved or error symbols are skipped so incomplete code does not create noisy analyzer output.

## Propagation

Named generic type restrictions propagate through generic base classes and interfaces when the restricted type argument maps directly to a derived type parameter. Generic method restrictions propagate through overrides, interface implementations, explicit interface implementations, and partial method counterparts by ordinal type-parameter mapping.

Signature propagation scans declaration signatures for direct mappings such as `Data<T>`, including nested signature containers such as `Action<Data<T>>`. It covers fields, properties, events, methods, parameters, return types, and generic constraints. It does not propagate through transformed arguments such as `Data<List<T>>`, `Data<T[]>`, or `Data<(T, string)>`.

## Cycle Handling

Signature propagation uses a graph keyed by named type original definition and type-parameter ordinal. Strongly connected components are reported once as `IVTS003` because v1 does not implement fixed-point contract reasoning through cycles.

Only generic-signature propagation cycles participate. Ordinary recursive object models do not produce cycle diagnostics.

## Cross-Assembly Metadata

The analyzer reads matching restriction attributes from referenced assemblies. This supports consuming annotated APIs from source projects, project references, and compiled packages as long as the final consuming project receives and loads the analyzer.

Malformed lookalike attributes in current source can produce `IVTS005`; metadata-only malformed lookalikes are ignored defensively when they cannot be attributed safely.

## NuGet and MSBuild Flow

The package contains:

- `analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.dll`
- `buildTransitive/IvTem.TypeSafety.props`

NuGet activates analyzer assets for direct package references. The `buildTransitive` props file also adds the same analyzer assembly for downstream projects that receive `IvTem.TypeSafety` transitively through normal package dependency flow.

The integration tests prove enforcement for:

- direct `PackageReference` to `IvTem.TypeSafety`;
- app project referencing a library project that normally references `IvTem.TypeSafety`;
- app package referencing a library package that normally depends on `IvTem.TypeSafety`.

Transparent enforcement still requires the final project build to receive and load the analyzer. If an intermediate project or package hides the dependency or excludes `buildTransitive`/analyzer assets, downstream enforcement is not claimed.
