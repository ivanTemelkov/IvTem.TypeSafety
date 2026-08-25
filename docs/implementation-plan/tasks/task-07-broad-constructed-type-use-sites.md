# Task 07: Broad constructed-type use sites

## Objective

Validate semantic uses of constructed generic types beyond object creation.

## Dependencies

- Task 04 complete.
- Task 05 complete.

## Expected affected files

- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/UseSites/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Build a reusable constructed-type validator that accepts an `INamedTypeSymbol` and optional type-argument syntax locations.
2. Register operation actions where symbols are reliable.
3. Add targeted syntax-node actions where operations do not expose type syntax cleanly.
4. Cover:
   - fields;
   - properties;
   - parameters;
   - return types;
   - `typeof`;
   - object creation;
   - target-typed `new`;
   - base and interface declarations;
   - casts;
   - `is` patterns;
   - `as`;
   - `default(Data<Exception>)`;
   - attribute `typeof(...)`;
   - constraints containing constructed generic types;
   - tuple element types;
   - collection expressions where target type is a restricted constructed generic.
5. Skip unresolved `IErrorTypeSymbol` and ambiguous/incomplete code.
6. De-duplicate operation and syntax diagnostics by source span plus generic argument ordinal.
7. Update progress documentation.

## Tests and checks

- One test group per supported use-site category.
- Explicit type argument diagnostics point at the offending argument where practical.
- Object creation is not the only detected use.
- Broken/incomplete code does not produce misleading diagnostics.
- Unsupported aliases/reflection/XML docs remain silent.
- `dotnet test --filter` for use-site tests.
- `dotnet build`.

## Acceptance criteria

- Supported semantic type-use locations produce `IVTS001`.
- Unsupported/deferred scenarios remain documented and silent.
- Duplicate diagnostics are avoided across registrations.

## Risks and open questions

- Broad use-site coverage can create overlap between operation and syntax callbacks.
- Some target-typed or pattern scenarios may not expose a constructed generic type directly.
- If this task is too large, split by use-site family while preserving the outline task.
