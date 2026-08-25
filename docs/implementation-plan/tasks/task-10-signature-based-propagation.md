# Task 10: Signature-based propagation

## Objective

Propagate restrictions through direct generic-parameter use in declaration signatures so wrappers cannot erase contracts.

## Dependencies

- Task 09 complete.
- Approval for nested containing-type generic parameter behavior.

## Expected affected files

- `src/IvTem.TypeSafety/Propagation/`.
- `src/IvTem.TypeSafety/Analysis/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/SignaturePropagation/`.
- `docs/implementation-plan/decisions.md`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Implement a signature scanner for named type declarations.
2. Inspect fields, properties, events, method parameters, method return types, and generic constraints.
3. Include private/public and static/instance members.
4. Traverse nested signature containers such as `Action<Data<T>>`.
5. Create propagation edges only when a restricted generic parameter maps directly to a generic parameter in scope.
6. Do not create edges for transformed arguments:
   - `Data<List<T>>`;
   - `Data<T[]>`;
   - `Data<(T, string)>`;
   - `Data<SomeWrapper<T>>`.
7. Do not inspect method bodies for contract propagation.
8. Apply approved nested generic type behavior for containing type parameters.
9. Update decisions and progress documentation.

## Tests and checks

- Field `Data<T>` propagates to wrapper `T`.
- Property `Data<T>` propagates.
- Method return and parameter `Data<T>` propagate.
- Event `Action<Data<T>>` propagates.
- Private and static members participate.
- Generic constraints containing `Data<T>` participate where Roslyn exposes them.
- No propagation from method body local `Data<T>`.
- No propagation through transformed type arguments.
- Nested type containing parameter scenarios match the approved decision.
- `dotnet test --filter` for signature propagation tests.
- `dotnet build`.

## Acceptance criteria

- Direct signature mappings produce inherited contracts on the containing generic declaration.
- Wrapper types with direct `Data<T>` signatures cannot be instantiated with forbidden types.
- Deferred transformed mappings remain unimplemented and documented.

## Risks and open questions

- Signature scanning can become expensive without caching.
- Nested containing-type behavior materially affects observable public contracts.
- Generic constraints may require different traversal from member types.
