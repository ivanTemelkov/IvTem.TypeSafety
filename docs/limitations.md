# Limitations

`IvTem.TypeSafety` v1 favors definite compile-time checks. Unsupported scenarios are usually left silent rather than diagnosed speculatively.

## Analyzer Activation

Restrictions are enforced only in projects where the analyzer is active. A project that references an annotated assembly but does not receive the analyzer will compile without `IvTem.TypeSafety` diagnostics.

## Transitive enforcement boundary

The tested and supported NuGet/MSBuild cases are:

- direct consumer `PackageReference` to `IvTem.TypeSafety`;
- project-reference consumers when the referenced project has a normal `PackageReference` to `IvTem.TypeSafety`;
- package-reference consumers when the referenced package has a normal dependency on `IvTem.TypeSafety`.

These cases are backed by temporary-project integration tests under `tests/IvTem.TypeSafety.Tests/Packaging/`.

Unsupported or unclaimed cases:

- an intermediate project uses `PrivateAssets="all"` or otherwise prevents `IvTem.TypeSafety` from flowing to consumers;
- a package excludes `buildTransitive` or analyzer assets from dependency flow;
- non-SDK-style or customized builds that do not import NuGet `buildTransitive` assets;
- consumers using a compiler older than the Roslyn API version referenced by the analyzer package.

The package currently references Roslyn `4.8.0` so it can load in older .NET 8 development environments.

## Deferred Analysis

The analyzer does not currently enforce:

- type alias declarations themselves;
- reflection or data-flow creation of generic types;
- XML documentation `cref` references;
- method-body-only contract propagation;
- generic lambda type parameters;
- cross-language source analysis beyond metadata consumed by Roslyn;
- transformed propagation mappings such as `Data<List<T>>`, `Data<T[]>`, `Data<(T, string)>`, or `Data<Wrapper<T>>`;
- chained generic constraints such as `where T : U where U : Exception`;
- special constraints such as `struct`, `unmanaged`, `class`, or `notnull` as proof of a restriction match.

Constructed generic use sites inside valid declarations and expressions are still analyzed when Roslyn exposes the constructed type or method symbol.

## Configuration Boundaries

`DisallowTypes(typeof(object))` is invalid because every type argument would be assignable to `object` under the supported matching rules. Use `DisallowExactTypes(typeof(object))` when only exact `object` should be rejected.

Open generic forbidden types and forbidden types containing generic parameters are invalid. Use closed concrete types such as `IEnumerable<string>` rather than `IEnumerable<>` or `IEnumerable<T>`.

## Cycle Handling

Cyclic generic-signature propagation is reported as `IVTS003` instead of being resolved. This applies only to cycles in the analyzer's generic restriction graph, not to ordinary recursive object references.
