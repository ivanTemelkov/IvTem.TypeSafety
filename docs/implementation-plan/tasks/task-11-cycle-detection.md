# Task 11: Cycle detection

## Objective

Detect cyclic generic-signature propagation graphs and report deterministic configuration errors instead of attempting fixed-point reasoning in v1.

## Dependencies

- Task 10 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Propagation/`.
- `src/IvTem.TypeSafety/Diagnostics/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/Cycles/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Define graph nodes as `(INamedTypeSymbol originalDefinition, typeParameterOrdinal)`.
2. Create edges only from generic-signature propagation relationships.
3. Exclude ordinary nongeneric recursive shapes.
4. Use deterministic symbol keys and source locations.
5. Detect cycles with DFS or strongly connected component analysis.
6. Report one `IVTS003` per cycle component.
7. Choose diagnostic location by deterministic ordering of source declarations.
8. Suppress use-site diagnostics that depend on unresolved cyclic propagation if needed to avoid cascades.
9. Update progress documentation.

## Tests and checks

- Simple two-type cycle reports one `IVTS003`.
- Longer cycle reports one deterministic `IVTS003`.
- Duplicate cycle paths do not create a diagnostic storm.
- Ordinary recursive nongeneric type is not reported.
- Analyzer does not stack overflow.
- Acyclic deep propagation remains valid.
- `dotnet test --filter` for cycle tests.
- `dotnet build`.

## Acceptance criteria

- Cycles in the v1 propagation graph fail deterministically.
- No infinite recursion or stack overflow occurs.
- Cycle diagnostics are not repeated at every affected use site.

## Risks and open questions

- The graph scope must remain narrow enough to avoid false positives.
- Metadata-only cycle participants may have limited diagnostic locations.
- Suppressing cascaded diagnostics must not hide unrelated direct violations.
