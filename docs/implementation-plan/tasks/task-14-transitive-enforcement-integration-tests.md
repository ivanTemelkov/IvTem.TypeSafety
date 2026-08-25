# Task 14: Transitive enforcement integration tests

## Objective

Prove or document the strongest practical NuGet/MSBuild transitive enforcement behavior.

## Dependencies

- Task 13 complete.

## Expected affected files

- `src/IvTem.TypeSafety/IvTem.TypeSafety.csproj`.
- Optional `buildTransitive/` package assets.
- `tests/IvTem.TypeSafety.Tests/Packaging/`.
- `docs/limitations.md`.
- `docs/architecture.md`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Investigate package asset flow for analyzers and source generators.
2. Add `buildTransitive` props/targets only if needed and testable.
3. Create temporary project integration test infrastructure.
4. Test direct package reference enforcement.
5. Test project reference scenario:
   - app references library project;
   - library uses `IvTem.TypeSafety`;
   - app consumes annotated library API.
6. Test package transitive scenario:
   - app references packaged Library A;
   - Library A depends on `IvTem.TypeSafety`;
   - app consumes annotated Library A API.
7. Clearly distinguish proven behavior from unsupported NuGet/MSBuild behavior.
8. Update documentation and progress.

## Tests and checks

- Direct package reference integration test.
- Project reference integration test.
- Package transitive integration test if technically feasible.
- Negative test documenting unsupported transparent transitivity if needed.
- `dotnet test --filter` for integration/package tests.
- `dotnet pack -c Release`.

## Acceptance criteria

- Every transitivity claim in docs is backed by a test.
- Unsupported scenarios are documented precisely.
- No automatic NuGet publication is added.

## Risks and open questions

- NuGet analyzer assets may not flow transitively in all desired scenarios.
- `buildTransitive` may help MSBuild assets but may not solve all analyzer/source-generator flow cases.
- Temporary integration projects can be slow or brittle; keep them focused.
