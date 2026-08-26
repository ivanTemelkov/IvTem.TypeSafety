# IvTem.TypeSafety architectural decisions

## Proposed decisions awaiting approval

### ADR-001: Hybrid Roslyn package

- Status: Proposed.
- Decision: Ship one NuGet package containing an incremental source generator and a diagnostic analyzer.
- Reasoning: The generator owns generated attributes; the analyzer owns semantic enforcement.
- Consequences: Testing must cover generator output and analyzer behavior independently and together.

### ADR-002: Zero runtime dependency package

- Status: Proposed.
- Decision: Package Roslyn components as analyzer/source-generator assets only, with no normal `lib/` runtime assembly.
- Reasoning: Consumers should receive embedded attributes in their compilations and no runtime dependency.
- Consequences: Package validation must inspect `.nupkg` contents.

### ADR-003: Attribute identity by metadata contract

- Status: Proposed.
- Decision: Recognize attributes by fully qualified metadata name and expected shape, not shared assembly identity.
- Reasoning: Attributes are embedded separately into each consuming compilation.
- Consequences: Malformed lookalike metadata handling needs explicit approval.

### ADR-004: Definite-only generic parameter reasoning

- Status: Proposed.
- Decision: Diagnose generic type parameters only when direct constraints definitively prove assignability.
- Reasoning: The spec excludes transitive constraint chains and special constraint reasoning in v1.
- Consequences: Some theoretically invalid generic flows remain allowed until future versions.

### ADR-005: Direct signature propagation only

- Status: Proposed.
- Decision: Propagate contracts through direct mappings such as `Data<T>`, including nested signature containers, but not transformed arguments such as `Data<List<T>>`.
- Reasoning: This blocks obvious wrappers while avoiding complex symbolic reasoning in v1.
- Consequences: Future versions can add richer propagation if the policy model remains explicit.

### ADR-006: Conservative cycle detection

- Status: Proposed.
- Decision: Treat cycles in the generic-signature propagation graph as `IVTS003` errors in v1.
- Reasoning: The specification explicitly avoids recursive/fixed-point propagation complexity.
- Consequences: The graph scope must be narrow enough to avoid diagnosing ordinary recursive object models.

### ADR-007: One diagnostic per offending generic argument

- Status: Proposed.
- Decision: Aggregate direct, inherited, and propagated matches into one `IVTS001` diagnostic per offending generic argument.
- Reasoning: This satisfies the duplicate-diagnostic requirement and keeps diagnostics actionable.
- Consequences: Diagnostic messages must list matched restrictions deterministically.

### ADR-008: Malformed current-source metadata diagnostics

- Status: Implemented pending approval.
- Decision: Implement `IVTS005` for attributes whose fully qualified metadata name is `IvTem.TypeSafety.DisallowTypesAttribute` or `IvTem.TypeSafety.DisallowExactTypesAttribute` but whose current-source metadata shape does not match the v1 contract.
- Reasoning: Task 03 requires defensive shape validation and the implementation plan preferred diagnostics for relevant current-compilation source rather than silently ignoring malformed lookalikes.
- Consequences: Referenced metadata-only malformed lookalikes may need refined handling during the cross-assembly task.

### ADR-009: Nested type signature propagation scope

- Status: Implemented pending approval.
- Decision: Propagate signature contracts only to generic parameters declared by the named type currently being analyzed. Containing type parameters used inside nested type signatures are not mapped onto the nested type's own contract.
- Reasoning: Nested constructed type use sites bind the inner type's generic arguments separately from the containing type's arguments, and v1 should avoid inventing cross-containing-type policy surfaces before cycle handling and package transitivity are validated.
- Consequences: `Outer<TOuter>.Inner<TInner>` can inherit contracts from `Data<TInner>` signatures, but `Data<TOuter>` signatures do not make `Inner<TInner>` reject `TInner`.

## Approved decisions

No implementation decisions have been formally approved yet.

## Release stabilization notes

### Task 17: v0.1.0 release candidate

- Status: Implemented pending human release approval.
- Decision: Treat the implemented diagnostic catalog `IVTS001` through `IVTS005` as the `0.1.0` shipped catalog, with `IVTS004` reserved but listed so future analyzer changes do not reuse the ID.
- Decision: Keep the runnable sample in the solution but mark it non-packable so solution-level `dotnet pack -c Release` produces only the analyzer/source-generator package.
- Decision: Do not add late behavior or performance rewrites during stabilization; document remaining unsupported scenarios and rely on the current compilation-start caches plus 108-test validation for the v0.1.0 release candidate.

### v0.1.1 symbol package layout fix

- Status: Implemented pending human release approval.
- Decision: Publish `0.1.1` because `0.1.0` was already published before nuget.org rejected the symbols package, and published NuGet package contents are immutable.
- Decision: Keep analyzer behavior unchanged and move the analyzer DLL package entry to `analyzers/dotnet/cs/netstandard2.0/` so it matches the generated `.snupkg` PDB path.

### v0.1.2 Roslyn 4.8 compatibility baseline

- Status: Implemented pending human release approval.
- Decision: Publish `0.1.2` with `Microsoft.CodeAnalysis.CSharp` `4.8.0` so older .NET 8 development environments can load the analyzer without `CS9057`.
- Decision: Drop target-typed collection-expression operation analysis for this baseline because Roslyn `4.8.0` does not expose `ICollectionExpressionOperation` or `OperationKind.CollectionExpression`.
