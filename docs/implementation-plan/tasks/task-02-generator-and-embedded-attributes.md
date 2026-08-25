# Task 02: Generator and embedded attributes

## Objective

Implement the incremental source generator that emits `IvTem.TypeSafety.DisallowTypesAttribute` and `IvTem.TypeSafety.DisallowExactTypesAttribute`.

## Dependencies

- Task 01 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Generation/`.
- `src/IvTem.TypeSafety/Attributes/` if generator constants or templates are separated.
- `tests/IvTem.TypeSafety.Tests/Generation/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Verify the selected Roslyn package exposes the expected embedded attribute APIs, including `AddEmbeddedAttributeDefinition()` where applicable.
2. Implement an `IIncrementalGenerator` with deterministic output.
3. Generate:
   - `internal sealed class DisallowTypesAttribute`;
   - `internal sealed class DisallowExactTypesAttribute`;
   - namespace `IvTem.TypeSafety`;
   - `[AttributeUsage(AttributeTargets.GenericParameter, AllowMultiple = true)]`;
   - constructor accepting `params Type[] types`;
   - XML documentation comments.
4. Use generated syntax compatible with broad consumer projects.
5. Do not generate global usings.
6. Do not add coexistence handling for manually defined conflicting attribute types.
7. Update progress documentation.

## Tests and checks

- Generator test confirming both generated sources exist.
- Semantic test confirming attributes can be applied to generic parameters.
- Test confirming generated attributes are internal.
- Test confirming `AllowMultiple = true`.
- Test confirming namespace and constructor shape.
- Test confirming no runtime attribute assembly reference is required by consumer code in the test compilation.
- `dotnet test --filter` for generator tests.
- `dotnet build`.

## Acceptance criteria

- Consumers can compile code using both attributes from generated source.
- Generated attributes match the specification.
- XML documentation is present in generated source.
- Manual duplicate type behavior is left to compiler errors.
- No analyzer diagnostics are implemented yet.

## Risks and open questions

- Embedded attribute API behavior may differ by Roslyn version.
- Source output must avoid language features that unintentionally raise consumer language-version requirements.
- Snapshot tests can become brittle; semantic assertions should be the main proof.
