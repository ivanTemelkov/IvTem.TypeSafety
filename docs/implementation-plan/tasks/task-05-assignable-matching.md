# Task 05: Assignable matching

## Objective

Enforce `DisallowTypesAttribute` for definite assignability relationships while excluding general C# convertibility.

## Dependencies

- Task 03 complete.
- Task 04 complete enough to share diagnostic aggregation and type normalization infrastructure.

## Expected affected files

- `src/IvTem.TypeSafety/Analysis/`.
- `src/IvTem.TypeSafety/Policies/`.
- `tests/IvTem.TypeSafety.Tests/Analysis/AssignableMatching/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Normalize `dynamic` to `System.Object`.
2. Erase nullable reference annotations.
3. Preserve `Nullable<T>` as a distinct constructed type.
4. Implement a definite assignability service.
5. First evaluate whether `Compilation.ClassifyConversion(actual, forbidden)` can safely identify implicit reference, boxing, variance, and array covariance without accepting numeric or user-defined conversions.
6. If `ClassifyConversion` is too broad, implement explicit symbol walking for:
   - same type;
   - base classes;
   - implemented interfaces;
   - constructed generic variance;
   - array covariance;
   - boxing/interface relationships for value types;
   - ref-like interface relationships.
7. For actual generic type parameters, inspect only direct class/interface constraints.
8. Do not inspect nested type arguments.
9. Emit one `IVTS001` per offending generic argument, aggregating matched forbidden types.
10. Update progress documentation.

## Tests and checks

- Same type rejected.
- Derived class rejected.
- Interface implementation rejected.
- Generic variance rejected.
- Array covariance rejected.
- `int` and `DateTime` rejected for forbidden `ValueType`.
- `int` rejected for forbidden `IComparable` when interface relationship applies.
- Ref-like type implementing forbidden interface rejected.
- User-defined implicit and explicit conversions do not match.
- Numeric conversions do not match.
- `Data<List<Exception>>` and `Data<Task<Exception>>` allowed for forbidden `Exception`.
- Generic parameter with direct `where T : Exception` rejected for forbidden `Exception`.
- Generic parameter without proving constraint allowed.
- Constraint chain `where T : U where U : Exception` remains allowed.
- `dotnet test --filter` for assignable matching tests.
- `dotnet build`.

## Acceptance criteria

- All required assignable relationships are diagnosed.
- General convertibility is not treated as assignability.
- Direct generic constraints prove violations only when the relationship is definite.
- Explicitly deferred constraint reasoning remains unimplemented and tested or documented.

## Risks and open questions

- Conversion classification can be subtle; tests must drive the final implementation choice.
- Ref-like interface behavior may require newer compiler references in tests.
- Variance and array covariance must be semantic, not syntax-based.
