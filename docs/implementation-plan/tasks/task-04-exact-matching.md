# Task 04: Exact matching

## Objective

Enforce `DisallowExactTypesAttribute` for direct policies at supported initial use sites.

## Dependencies

- Task 03 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Analysis/`.
- `src/IvTem.TypeSafety/Policies/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/ExactMatching/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Implement type normalization for exact matching:
   - treat `dynamic` as `System.Object`;
   - erase nullable reference annotations;
   - preserve nullable value types as distinct constructed types.
2. Use semantic symbol identity rather than syntax spelling.
3. Match exact forbidden types only; do not match derived classes or interface implementations.
4. Do not use generic constraints as proof of exact type identity.
5. Emit one `IVTS001` per offending generic argument.
6. Start with direct explicit constructed type uses needed to prove the algorithm, then leave broader use-site coverage to Tasks 06 and 07.
7. Update progress documentation.

## Tests and checks

- `Data<Exception>` rejected for `DisallowExactTypes(typeof(Exception))`.
- `Data<InvalidOperationException>` allowed for exact `Exception`.
- `string` and `string?` both rejected for exact `string`.
- `dynamic` rejected for exact `object`.
- `int?` allowed for exact `int`.
- Generic parameter constrained to `Exception` is not rejected by exact `Exception`.
- Alias spelling such as `int` and `System.Int32` resolves by semantic identity.
- `dotnet test --filter` for exact matching tests.
- `dotnet build`.

## Acceptance criteria

- Exact matching semantics from the specification are implemented and tested.
- Nullable reference annotations cannot bypass exact restrictions.
- Nullable value types remain distinct.
- No assignability matching is implemented in this task.

## Risks and open questions

- `SymbolEqualityComparer.Default` behavior around nullable annotations must be verified by tests.
- Type forwarding should be left to Roslyn unless a test exposes incorrect behavior.
