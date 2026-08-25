# Task 08: Override and interface member contract propagation

## Objective

Propagate generic parameter restrictions through overridden members and interface implementations.

## Dependencies

- Task 03 complete.
- Task 06 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Propagation/`.
- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/MemberPropagation/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Add a member contract provider for `IMethodSymbol` and other generic members if needed.
2. For overrides, read contracts from `OverriddenMethod` and map type parameters by ordinal.
3. For interface implementations, use Roslyn implementation relationship APIs instead of matching names by text.
4. Include explicit interface implementations.
5. Include generic methods with multiple metadata paths.
6. Include partial methods where both declaration and implementation exist.
7. Combine direct and inherited member contracts as a union.
8. Do not physically copy attributes to implementation symbols.
9. Ensure use-site validation of the implementing member sees inherited contracts.
10. Update progress documentation.

## Tests and checks

- Interface generic method contract enforced on implementation without copied attribute.
- Override generic method contract enforced on override.
- Explicit interface implementation contract enforced.
- Multiple interface contracts are unioned.
- Partial method declaration/implementation behavior is deterministic.
- Duplicate diagnostics are not emitted through multiple inherited paths.
- `dotnet test --filter` for member propagation tests.
- `dotnet build`.

## Acceptance criteria

- Restrictions behave as semantic contracts across overrides and interface implementations.
- Direct and inherited member policies are accumulated.
- Diagnostics remain one per offending generic argument.

## Risks and open questions

- Roslyn interface implementation APIs can require careful handling for explicit implementations.
- Multiple interface paths can create duplicate policy sources.
- Metadata-only inherited members may not provide source locations.
