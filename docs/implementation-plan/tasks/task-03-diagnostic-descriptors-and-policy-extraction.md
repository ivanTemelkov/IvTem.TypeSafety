# Task 03: Diagnostic descriptors and policy extraction

## Objective

Create the diagnostic catalog and implement parsing of direct `IvTem.TypeSafety` attributes into immutable policy objects.

## Dependencies

- Task 02 complete.
- Diagnostic catalog reviewed enough to implement stable IDs.

## Expected affected files

- `src/IvTem.TypeSafety/Diagnostics/`.
- `src/IvTem.TypeSafety/Policies/`.
- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/`.
- `docs/implementation-plan/decisions.md`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Add diagnostic descriptors for approved diagnostics.
2. Implement metadata-name recognition for:
   - `IvTem.TypeSafety.DisallowTypesAttribute`;
   - `IvTem.TypeSafety.DisallowExactTypesAttribute`.
3. Validate attribute shape defensively:
   - derives from `System.Attribute`;
   - constructor argument is representable as `System.Type[]`;
   - applied to generic parameter symbols.
4. Parse all direct attributes on a generic parameter.
5. Support multiple attributes and multiple types.
6. De-duplicate semantically equivalent configured types per restriction kind.
7. Report configuration diagnostics for:
   - empty type list;
   - null array or null entries where Roslyn exposes them;
   - open/unbound generic types;
   - forbidden types containing generic parameters from the surrounding declaration;
   - `DisallowTypes(typeof(object))`.
8. Keep policy extraction independent from use-site enforcement.
9. Update progress and decision logs.

## Tests and checks

- Valid direct `DisallowTypes` extraction.
- Valid direct `DisallowExactTypes` extraction.
- Multiple attributes are accumulated.
- Multiple constructor arguments are accumulated.
- Duplicate configured types are de-duplicated.
- Empty/null/open-generic/surrounding-generic/object-invalid cases report expected diagnostics.
- `dotnet test --filter` for policy/configuration tests.
- `dotnet build`.

## Acceptance criteria

- Valid policies are represented deterministically.
- Invalid direct configurations produce configuration errors.
- No forbidden generic argument enforcement is required yet beyond configuration validation.
- Diagnostic IDs, titles, and messages are centralized.

## Risks and open questions

- `IVTS005` behavior for malformed lookalike metadata needs approval.
- Roslyn `TypedConstant` shapes for `params Type[]` null cases require empirical tests.
- Source locations for metadata-only invalid configurations may be limited.
