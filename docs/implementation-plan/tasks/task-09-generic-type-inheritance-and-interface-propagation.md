# Task 09: Generic type inheritance and interface propagation

## Objective

Propagate generic type parameter restrictions through generic base classes and interfaces, including transitive inheritance.

## Dependencies

- Task 07 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Propagation/`.
- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/TypePropagation/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Build a generic type contract provider keyed by original type definition and type parameter ordinal.
2. Inspect base type and interface constructed types.
3. Map restricted base/interface type parameters to derived type parameters by actual type argument.
4. Support reordered mappings such as `IBase<B, A>`.
5. Support repeated mappings such as `SomeType<T, T>` by unioning policies.
6. Support multiple interfaces and transitive propagation.
7. Report immediate violations when a base/interface declaration supplies a concrete forbidden type.
8. Combine partial declaration attributes by generic parameter ordinal.
9. Cache results per compilation and detect recursion boundaries.
10. Update progress documentation.

## Tests and checks

- Generic interface implementation propagation.
- Generic base class propagation.
- Multiple inherited contracts unioned.
- Transitive inheritance propagation.
- Partial declarations combine restrictions.
- Reordered generic parameter mapping.
- Repeated generic parameter mapping.
- Concrete forbidden type in base/interface declaration diagnosed immediately.
- Duplicate diagnostics avoided for diamond-like paths.
- `dotnet test --filter` for type propagation tests.
- `dotnet build`.

## Acceptance criteria

- Derived generic types inherit base/interface generic contracts through direct mappings.
- Transitive inherited policies are enforced at use sites.
- Violations in base/interface declarations are reported at declaration time.

## Risks and open questions

- Recursive inheritance graphs must not cause unbounded traversal.
- Multiple interface paths can produce duplicate policy sources.
- Mapping must be symbol-based and robust across metadata references.
