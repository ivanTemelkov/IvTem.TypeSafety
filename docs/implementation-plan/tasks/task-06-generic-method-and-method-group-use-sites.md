# Task 06: Generic method and method-group use sites

## Objective

Validate explicit and inferred generic method arguments, method groups, delegates, and local functions.

## Dependencies

- Task 04 complete.
- Task 05 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/GenericMethods/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Register operation analysis for generic invocations.
2. Extract constructed `IMethodSymbol` instances after type inference.
3. Map method type parameters to actual type arguments by ordinal.
4. Validate direct policies on method generic parameters.
5. Support explicit type argument locations when syntax exists.
6. For inferred type arguments, report on the invocation or method name location and include the inferred offending type in the message.
7. Analyze method groups and delegate creation where a closed generic method is formed.
8. Include local function symbols where Roslyn exposes generic local functions.
9. De-duplicate diagnostics when a method group also appears in conversion/delegate operations.
10. Update progress documentation.

## Tests and checks

- Explicit generic method argument violation.
- Inferred generic method argument violation.
- Inferred allowed generic argument.
- Method group assigned to delegate with forbidden closed generic argument.
- Generic local function explicit and inferred violations.
- Multiple restrictions collapse to one diagnostic per method type argument.
- Diagnostic location is precise for explicit arguments.
- Diagnostic message includes inferred type where no explicit argument exists.
- `dotnet test --filter` for generic method tests.
- `dotnet build`.

## Acceptance criteria

- Explicit and inferred generic method arguments are validated.
- Closed generic method groups are diagnosed even if not invoked.
- Local functions are covered if supported by Roslyn symbols.
- Duplicate diagnostics are controlled.

## Risks and open questions

- Operation trees for method groups can vary by compiler version.
- Generic lambda support remains unresolved and should not be folded into this task unless approved.
- Inferred argument locations are inherently less precise than explicit syntax.
