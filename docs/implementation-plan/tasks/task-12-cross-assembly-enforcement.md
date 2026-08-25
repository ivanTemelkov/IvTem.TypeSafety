# Task 12: Cross-assembly enforcement

## Objective

Enforce restrictions declared in referenced assemblies without relying on shared CLR identity for generated attributes.

## Dependencies

- Tasks 03 through 11 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Policies/`.
- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/CrossAssembly/`.
- `tests/IvTem.TypeSafety.Tests/TestInfrastructure/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Extend policy extraction to metadata symbols.
2. Recognize attributes by stable metadata name and validated shape.
3. Ensure direct, inherited, and propagated policies can be read from referenced assemblies.
4. Do not require source locations for metadata policy sources.
5. Handle malformed lookalike metadata according to the approved `IVTS005` decision.
6. Support metadata from non-C# assemblies when the metadata contract matches.
7. Skip unresolved/error metadata symbols defensively.
8. Add tests with separate compilations and metadata references.
9. Update progress documentation.

## Tests and checks

- Assembly A declares annotated generic type, Assembly B uses forbidden type argument and gets `IVTS001`.
- Embedded attributes from separate compilations are recognized despite different assembly identities.
- Referenced generic method contracts are enforced.
- Referenced propagated type contracts are enforced.
- Malformed lookalike metadata follows approved behavior.
- Generated declaration source from another generator is consumed by user-authored code and enforced when symbol metadata is available.
- Cross-language metadata behavior is documented or tested if practical.
- `dotnet test --filter` for cross-assembly tests.
- `dotnet build`.

## Acceptance criteria

- Cross-assembly use-site enforcement works from metadata.
- Attribute identity does not depend on a shared runtime assembly.
- Metadata-only sources do not cause location or null-reference failures.

## Risks and open questions

- Metadata symbols can lack source locations and detailed attribute usage metadata.
- Malformed lookalike handling can create false positives if too aggressive.
- Cross-language metadata tests may require tooling not available locally.
