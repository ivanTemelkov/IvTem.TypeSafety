# IvTem.TypeSafety implementation plan

## 1. Repository and convention findings

- Repository root: `C:\z_mySource\IvTem.TypeSafety`.
- Current branch: `main`.
- Latest local commit at planning time: `ce1556f chore: Added instructions`.
- Remote: `https://github.com/ivanTemelkov/IvTem.TypeSafety.git`.
- Existing tracked files: `.gitignore`, `CODING-STYLE.md`, `Instructions.md`, `LICENSE`.
- `docs/agent/` exists but contains no files. `CODING-STYLE.md` does not currently reference any concrete additional convention document to read.
- `LICENSE` is already MIT with copyright year 2026 and author Ivan Temelkov.

Authoritative coding conventions:

- File-scoped namespaces.
- Namespace mirrors folder path.
- One public type per file.
- Allman braces, 4 spaces, no tabs.
- No `this.`.
- Boolean negation uses `== false`.
- Private fields are `camelCase` with no underscore; prefer private auto-properties for dependencies/state.
- `sealed` by default.
- Nullable reference types enabled and warning-free.
- Treat warnings as errors.
- Explicit `StringComparison` and `StringComparer`.
- Async methods do not use an `Async` suffix unless needed to disambiguate.
- Await calls use explicit `.ConfigureAwait(continueOnCapturedContext: false)` for library code.

Planning/specification conflicts or ambiguities:

- The style guide says target platform is .NET 10 / C# latest, while the analyzer/generator assembly must target `netstandard2.0`. Recommendation: repository build tooling targets .NET 10, analyzer package assembly targets `netstandard2.0`, generated source avoids unnecessary new language features.
- The style guide says some projects centralize common imports in `GlobalUsings.cs`, but analyzer projects targeting `netstandard2.0` and source generators often benefit from explicit usings for portability. Recommendation: use explicit usings in analyzer/generator code unless repetition becomes excessive.
- The spec requests use of Roslyn embedded-attribute support including `AddEmbeddedAttributeDefinition()`. Exact API availability and behavior must be confirmed against the selected `Microsoft.CodeAnalysis.CSharp` package version during Task 2 before finalizing generator implementation.
- The spec requires broad semantic use-site coverage and also says not to analyze generated code. Recommendation: skip diagnostics whose primary source location is generated code, but still read symbol metadata from generated declarations when user-authored code consumes them.

## 2. Proposed solution and project structure

Initial repository structure after approval:

```text
IvTem.TypeSafety.slnx
Directory.Build.props
Directory.Packages.props
README.md
CHANGELOG.md
AnalyzerReleases.Shipped.md
AnalyzerReleases.Unshipped.md
src/
  IvTem.TypeSafety/
    IvTem.TypeSafety.csproj
    Attributes/
    Diagnostics/
    Generation/
    Analysis/
    Policies/
    Propagation/
    Packaging/
tests/
  IvTem.TypeSafety.Tests/
    IvTem.TypeSafety.Tests.csproj
    Generation/
    Analysis/
    Packaging/
    TestInfrastructure/
samples/
  IvTem.TypeSafety.Sample/
    IvTem.TypeSafety.Sample.csproj
docs/
  implementation-plan/
  diagnostics.md
  architecture.md
  limitations.md
.github/
  workflows/
    ci.yml
```

Notes:

- The single production project is `src/IvTem.TypeSafety/IvTem.TypeSafety.csproj`.
- It produces the analyzer/source-generator assembly and packages it only as analyzer assets.
- The project must not contribute a `lib/` runtime asset to the NuGet package.
- Tests will include Roslyn in-memory analyzer/generator tests and package-level integration tests using temporary projects.
- `README.md` must be the NuGet package README and visibly state the project is AI-assisted and built with OpenAI Codex.

## 3. Proposed Roslyn architecture

Use a hybrid Roslyn package:

- `IIncrementalGenerator` generates embedded internal attributes.
- `DiagnosticAnalyzer` enforces semantic rules.
- Shared helper types inside the analyzer assembly represent parsed policies and matching results.
- No `CodeFixProvider` in v1.
- C# source only for analysis registrations.

High-level analyzer flow:

1. Register a compilation start action.
2. Initialize well-known symbol references and immutable caches.
3. Build a policy provider capable of reading direct attributes and inherited/propagated contracts.
4. Register symbol actions for generic declarations and signature propagation graph validation.
5. Register operation and syntax-node actions for constructed type and generic method use sites where Roslyn exposes useful semantic symbols.
6. De-duplicate diagnostics per offending generic argument and per cycle.

## 4. Attribute-generation strategy

Generate source under namespace `IvTem.TypeSafety`:

- `internal sealed class DisallowTypesAttribute : Attribute`
- `internal sealed class DisallowExactTypesAttribute : Attribute`
- `[AttributeUsage(AttributeTargets.GenericParameter, AllowMultiple = true)]`
- Constructor accepting `params Type[] types`.
- XML documentation comments for IntelliSense.
- Embedded attribute support using Roslyn's embedded-attribute mechanism.

Generated source should use portable syntax:

- Block-scoped namespace is acceptable for widest compatibility, but file-scoped namespace matches repo style. Recommendation: generated source may use block-scoped namespace if required by embedding pattern; production hand-written code uses file-scoped namespaces.
- Avoid collection expressions and other C# 12+ constructs in generated source.

Manual consumer conflicts:

- Do not implement coexistence logic for user-defined `IvTem.TypeSafety.DisallowTypesAttribute` or `DisallowExactTypesAttribute`.
- Let compiler duplicate-type conflicts surface naturally.

## 5. Analyzer registration strategy

Recommended registrations:

- `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)`.
- `context.EnableConcurrentExecution()`.
- `CompilationStartAction` for per-compilation caches.
- `SymbolAction` on `NamedType`, `Method`, and possibly `Parameter`/`Event` owners to validate declaration-time configuration and build propagation contracts.
- `OperationAction` for `ObjectCreation`, `Invocation`, `DelegateCreation`, `Conversion`, `TypeOf`, `FieldReference`, `PropertyReference`, `MethodReference`, and related operations where constructed symbols are available.
- Targeted `SyntaxNodeAction` only where operations do not expose the exact type-argument location, such as base lists, type syntax in declarations, constraints, casts, patterns, and `typeof`.

Rationale:

- Symbol actions are better for declaration contracts, partial declarations, overrides, interface implementations, and signature propagation.
- Operation actions catch inferred generic method arguments and semantic uses that are not simple syntax names.
- Syntax-node actions are used for precise locations, not for name-based correctness.

## 6. Policy representation

Internal immutable model:

```text
RestrictionPolicy
  ownerSymbol
  typeParameterSymbol
  typeParameterOrdinal
  sourceKind: Direct | Override | Interface | BaseType | SignaturePropagation
  disallowAssignable: ImmutableArray<ForbiddenType>
  disallowExact: ImmutableArray<ForbiddenType>

ForbiddenType
  typeSymbol
  displayName
  sourceLocation
  declarationOrder
```

Rules:

- Attribute identity is based on metadata name, namespace, constructor shape, and target generic parameter.
- Duplicate configured types are de-duplicated per policy kind using `SymbolEqualityComparer.IncludeNullability` only where nullability is intentionally significant. Recommendation: use `SymbolEqualityComparer.Default` for reference nullability erasure and exact runtime-type identity, while preserving nullable value type distinction because `Nullable<T>` is a distinct named type.
- Diagnostics maintain deterministic forbidden type ordering by first declaration order, then fully qualified metadata display name as a tie breaker.

## 7. Assignability algorithm

The assignable check must reject only definite relationships.

Recommended algorithm:

1. Normalize `dynamic` to `System.Object`.
2. Erase nullable reference annotations for comparison.
3. Do not unwrap `Nullable<T>`.
4. If actual type is an ordinary concrete/interface/array type, use Roslyn conversion classification or symbol APIs that represent implicit reference, boxing, variance, and array covariance, while excluding user-defined and numeric conversions.
5. Prefer `Compilation.ClassifyConversion(actual, forbidden)` only if tests prove it distinguishes acceptable implicit reference/boxing conversions from user-defined/numeric conversions. Otherwise implement explicit symbol walking for base types, interfaces, variance, arrays, and boxing interfaces.
6. If actual type is a generic parameter, inspect only direct constraints:
   - A direct class/interface constraint assignable to the forbidden type proves violation.
   - Do not follow `where T : U` chains.
   - Do not infer from `struct`, `unmanaged`, `class`, or `notnull`.
7. Treat ref-like type interface implementation as assignable for purposes of interface restrictions.
8. Do not recursively inspect arbitrary nested generic arguments.

Acceptance point for Task 6: create tests that prove user-defined conversions and numeric conversions do not match even if `ClassifyConversion` is used internally.

## 8. Exact-match algorithm

The exact check:

- Normalizes `dynamic` to `System.Object`.
- Erases nullable reference annotation differences.
- Preserves `Nullable<T>` as a distinct constructed type.
- Uses semantic symbol identity, not syntax spelling.
- Does not treat generic constraints as exact identity.

Examples:

- `string` and `string?` both match forbidden `string`.
- `dynamic` matches forbidden `object`.
- `int?` does not match forbidden `int`.
- `InvalidOperationException` does not match forbidden `Exception`.

## 9. Propagation graph design

Graph node:

- A generic declaration parameter contract identified by `(containing generic symbol, type parameter ordinal)`.
- Named types and generic methods participate.
- Non-generic members are scanned only to find signature edges from their containing generic type parameters.

Propagation edge:

- From a restricted generic parameter of a constructed generic use in a declaration signature to the directly mapped generic parameter supplied as its type argument.
- Example: in `Wrapper<T>.Value : Data<T>`, edge `Data<TPayload> -> Wrapper<T>`.
- Base/interface edges use the same direct type-argument mapping.
- Override/interface member implementation edges map overridden/interface method generic parameter contracts to implementing method generic parameters by ordinal and explicit implementation relationship.
- Direct mapping through reordering is supported. Example: `Derived<A, B> : IBase<B, A>` maps `IBase<T1>` to `Derived<B>` and `IBase<T2>` to `Derived<A>`.
- Repeated mappings combine policies. Example: `SomeType<T, T>` maps both restricted positions to the same containing `T`, producing one combined contract.

Participating signature positions:

- Fields.
- Properties.
- Events.
- Method parameters.
- Method return types.
- Generic constraints containing constructed generic types.
- Static and instance members.
- Public, internal, protected, private members.
- Nested signature containers such as `Action<Data<T>>`.

Non-participating v1 positions:

- Method bodies for contract propagation.
- Type aliases.
- XML documentation `cref`.
- Reflection/data-flow constructs.
- Nested transformations such as `Data<List<T>>`, `Data<T[]>`, or `Data<(T, string)>`.

Nested generic types:

- Recommendation: allow propagation from member signatures to any directly referenced containing or nested generic parameter that is in scope.
- `Outer<T>.Inner` with `Data<T>` in `Inner` signature should propagate to `Outer<T>` because `T` is directly mapped and part of the containing type contract.
- `Outer<T>.Inner<U>` with `Data<T>` and `Data<U>` should propagate separately to `Outer<T>` and `Inner<U>`.
- This should be confirmed because it affects observable diagnostics for nested types.

## 10. Cycle-detection design

Cycle graph:

- Nodes are generic named-type parameter contracts `(INamedTypeSymbol originalDefinition, ordinal)`.
- Edges are direct signature-propagation edges between named-type parameter contracts.
- Method generic parameters are excluded from cycle detection unless method signature propagation later creates recursive method-contract graphs; v1 should avoid that complexity.
- Edges are created only for declaration signatures, not method bodies.

Cycle detection:

- Build an adjacency list during compilation start on demand.
- Run depth-first search with visited/visiting states.
- Use original definitions and ordinals as stable keys.
- Report one `IVTS003` diagnostic per strongly connected component, at the first deterministic source location among participating declarations.
- Sort cycle members by metadata name and ordinal before choosing location and message.
- Do not report repeated cycle diagnostics for every use site affected by the cycle.

Important boundary:

- Ordinary recursive concrete type shapes should not be reported unless they create a generic-parameter propagation edge. `class Node { Node? Next; }` is not a graph participant.

## 11. Cross-assembly metadata design

Attribute recognition:

- Match metadata name exactly:
  - `IvTem.TypeSafety.DisallowTypesAttribute`
  - `IvTem.TypeSafety.DisallowExactTypesAttribute`
- Validate expected shape defensively:
  - Type derives from `System.Attribute`.
  - Attribute usage targets generic parameters when metadata is available.
  - Constructor supplies `params Type[]` or equivalent metadata array of `System.Type`.
- Do not require CLR identity with this package's generated attributes because attributes are embedded per compilation.

Referenced assemblies:

- Read `AttributeData` from metadata symbols.
- Enforce use-site violations in consuming compilations when analyzer is active.
- If metadata has same names but malformed shape, report a configuration diagnostic only when the malformed attribute is applied to a relevant generic parameter and enough metadata exists to locate or cite it. Otherwise ignore defensively to avoid false positives.

Cross-language metadata:

- Recommendation: support metadata produced by any .NET language if it contains the expected attribute contract. Do not add Visual Basic source analysis.

## 12. Packaging and transitivity design

NuGet goals:

- Package ID: `IvTem.TypeSafety`.
- Version: `0.1.2`.
- Authors: `Ivan Temelkov`.
- License: MIT.
- Repository URL from Git remote: `https://github.com/ivanTemelkov/IvTem.TypeSafety`.
- No `lib/` runtime assembly.
- Analyzer/generator asset under `analyzers/dotnet/cs/netstandard2.0/`.
- README embedded as package README.
- Symbol package `.snupkg`.
- Deterministic builds and Source Link.

Transitivity plan:

- Investigate analyzer transitivity using `buildTransitive` props/targets and package asset metadata.
- Add package integration tests for:
  - direct PackageReference consumer.
  - project reference from app to library using source analyzer.
  - app -> Library A -> package, where Library A exposes annotated API.
  - app -> Library A package -> `IvTem.TypeSafety` transitive if feasible.
- Do not claim transparent enforcement unless tests prove the scenario.
- Document any case where NuGet/MSBuild does not flow analyzers to downstream consumers.

## 13. Proposed diagnostic catalog

All diagnostics are `DiagnosticSeverity.Error` and use standard analyzer suppression/configuration.

| ID | Title | Stable contract | Message shape | Location |
| --- | --- | --- | --- | --- |
| `IVTS001` | Forbidden generic argument | Yes | `Type argument '{0}' is not allowed for generic parameter '{1}' of '{2}'. Matched restriction(s): {3}.` | Explicit type argument syntax when available; otherwise invocation/member/method-group location. |
| `IVTS002` | Invalid type-safety attribute configuration | Yes | `Invalid {0} configuration on generic parameter '{1}': {2}.` | Invalid attribute argument or attribute syntax; fallback to generic parameter. |
| `IVTS003` | Cyclic type-safety contract propagation | Yes | `Cyclic generic type-safety contract propagation detected among: {0}. Cycles are not supported in v1.` | First deterministic source declaration participating in the cycle. |
| `IVTS004` | Contradictory generic parameter restriction | Yes | `Generic parameter '{0}' has a type-safety restriction that contradicts its direct constraints: {1}.` | Generic parameter or direct constraint clause. |
| `IVTS005` | Malformed type-safety attribute metadata | Tentative | `Attribute '{0}' has the IvTem.TypeSafety metadata name but does not match the expected v1 contract: {1}.` | Attribute application when source exists; otherwise containing symbol. |

Catalog notes:

- `IVTS001` collapses direct, inherited, and signature-propagated matches into one diagnostic per offending generic argument.
- `IVTS002` covers empty type list, null entry/null array where representable, open generic, type containing generic parameters, and `DisallowTypes(typeof(object))`.
- `IVTS004` is distinct from `IVTS002` because the attribute syntax may be valid but the declaration contract is impossible.
- `IVTS005` needs review. It may be too noisy for unrelated lookalike metadata; fallback is to ignore malformed lookalikes unless they are source-authored in the current compilation.

## 14. Complete test strategy

Test infrastructure:

- Use xUnit.
- Use `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` / `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing` or a small custom harness if the official test framework lags .NET 10/C# 14.
- Keep diagnostics asserted by ID, message arguments, and precise source span where practical.
- Use temporary project integration tests for packaging/transitivity.

Generator tests:

- Generates both attributes.
- Namespace is `IvTem.TypeSafety`.
- Attributes are internal.
- `AttributeUsage` targets generic parameters and allows multiple.
- Constructor accepts `params Type[]`.
- Embedded attribute behavior works.
- XML documentation is emitted.
- Manual duplicate type conflicts are left to compiler behavior.

Assignable tests:

- Same class.
- Derived class.
- Interface implementation.
- Generic variance.
- Array covariance.
- Boxing/value types against `ValueType` and interfaces.
- Ref-like type implementing forbidden interface.
- User-defined implicit/explicit conversions do not match.
- Numeric conversions do not match.
- `Data<List<Exception>>` and `Data<Task<Exception>>` are allowed for forbidden `Exception`.

Exact tests:

- Exact class rejected.
- Derived class accepted.
- Nullable reference annotation does not bypass.
- `dynamic` behaves as `object`.
- Nullable value type remains distinct.
- Aliases such as `int`/`System.Int32` and `nint`/`System.IntPtr` resolve by semantic identity.

Invalid configuration tests:

- Empty attribute.
- Null argument and null array shapes where C# can represent them.
- Open/unbound generic.
- Forbidden type containing generic parameter.
- `DisallowTypes(typeof(object))`.
- Contradictory declaration from direct class/interface constraints.
- Malformed lookalike attribute metadata.

Generic method tests:

- Explicit generic arguments.
- Inferred generic arguments.
- Method groups.
- Delegates.
- Local functions.
- Overrides and interface implementations for generic methods.
- Explicit interface implementations.
- Partial methods.

Use-site tests:

- Fields.
- Properties.
- Parameters.
- Return types.
- `typeof`.
- Object creation.
- Target-typed `new`.
- Base/interface declarations.
- Casts.
- `is` patterns.
- `as`.
- `default(Data<Exception>)`.
- Attribute `typeof(...)`.
- Constraints containing constructed generic types.
- Tuple element types.
- Collection expressions where target type is restricted constructed generic.

Propagation tests:

- Generic interface implementation.
- Generic base class.
- Multiple interfaces.
- Transitive inheritance.
- Partial declarations.
- Direct signature propagation.
- Private members.
- Static members.
- Nested signature containers such as `Action<Data<T>>`.
- Transitive direct-mapping propagation.
- Reordered generic mappings.
- Repeated generic mappings.
- Nested generic type containing parameters.
- No propagation from method bodies.
- No propagation through `Data<List<T>>`, `Data<T[]>`, tuple transformations, or wrappers.

Cycle tests:

- Simple two-node cycle.
- Longer cycle.
- Deterministic diagnostic location and message.
- No stack overflow.
- No duplicate diagnostic storm.
- Ordinary recursive nongeneric type is not reported.

Cross-assembly and package tests:

- Declaration and consumption in separate in-memory compilations.
- Metadata attribute recognition without shared CLR identity.
- Project reference scenario.
- Direct package reference scenario.
- Transitive package scenario where feasible.
- Generated declaration consumed by user-authored source.

Deferred behavior tests/documentation assertions:

- Type alias enforcement is silent.
- Reflection-created generic types are silent.
- XML documentation `cref` is silent.
- Generic constraint chains are not followed.
- Special constraints are not used as proof.
- Unsupported future scenarios are documented rather than diagnosed.

## 15. Performance strategy

- Enable concurrent analyzer execution.
- Cache policy extraction per original generic symbol.
- Cache type relationship checks by `(actual, forbidden, mode)`.
- Use immutable collections and deterministic ordering.
- Avoid whole-compilation syntax scans where symbol start actions can provide declarations.
- Bound propagation traversal and terminate cycles early.
- Add synthetic large-compilation tests with many generic declarations, many use sites, and deep but acyclic propagation.
- Track allocation-heavy paths in code review even without BenchmarkDotNet.

## 16. Documentation strategy

Required docs:

- `README.md`: overview, AI-assisted Codex statement, installation, basic examples, diagnostics summary, limitations.
- `docs/architecture.md`: generator/analyzer split, policy model, propagation graph, cycle handling.
- `docs/diagnostics.md`: complete diagnostic catalog and examples.
- `docs/limitations.md`: deferred v1 features.
- `CHANGELOG.md`: 0.1.0 initial release notes, 0.1.1 symbol-package layout fix, and 0.1.2 Roslyn compatibility fix.
- `AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`.
- `docs/implementation-plan/progress.md`: task status, validation, decisions, lessons, unresolved issues, deferred features.
- `docs/implementation-plan/decisions.md`: architectural decisions and approval state.

## 17. Sample strategy

Create `samples/IvTem.TypeSafety.Sample`:

- Demonstrates `DisallowTypes` with `Exception`.
- Demonstrates `DisallowExactTypes` with exact `object` or `string`.
- Includes generic class, struct, interface, delegate, method, and local function examples.
- Keeps runnable code valid.
- Places intentionally invalid examples in comments or separate excluded files documented in the sample README.
- Sample validation runs in CI.

## 18. CI and package-validation strategy

GitHub Actions workflow:

- Restore with locked dependency versions.
- Build Release on .NET 10.
- Run tests on .NET 10.
- Run selected compatibility tests on .NET 8 where feasible.
- Pack Release.
- Validate `.nupkg` contents:
  - no `lib/` runtime assembly.
  - analyzer asset exists under `analyzers/dotnet/cs/`.
  - README included.
  - Source Link and symbols produced.
- Run sample build.
- No NuGet publication in v1.

## 19. Section 57 unresolved questions and recommended v1 behavior

1. Generic lambdas: recommend out of scope unless current C# and Roslyn APIs clearly support attributes on generic lambda type parameters. Document as deferred if unsupported or awkward.
2. Records: support naturally because record classes/structs are named type symbols with generic parameters.
3. Nested generic types: recommend direct propagation to any in-scope containing or nested generic parameter used directly in a signature.
4. Generic parameter reordering: support direct ordinal mapping from constructed base/interface/member signatures.
5. Repeated mappings: union policies and emit one diagnostic per supplied argument.
6. Explicit interface implementations and partial methods: support through Roslyn override/interface implementation symbol relationships; location falls back to implementation declaration if no explicit type argument exists.
7. Additional semantic use locations: include the list in section 14 for v1 tests; remain silent only for aliases, reflection, and XML docs.
8. Pointer/function-pointer/unsafe: follow compiler limitations; analyze constructed generic types inside valid unsafe signatures if Roslyn exposes them as type symbols.
9. Error symbols/incomplete code: skip diagnostics involving `IErrorTypeSymbol`, unresolved metadata, or ambiguous binding.
10. Malformed embedded metadata: prefer defensive `IVTS005` only for relevant current-compilation source; otherwise ignore lookalikes.
11. Attribute identity: metadata name plus shape validation, not shared assembly identity.
12. Diagnostic precedence: one `IVTS001` per offending argument; message can cite direct/inherited/propagated sources in deterministic order.
13. Matched restriction ordering: first declaration order, then fully qualified metadata name.
14. Null attribute arguments: test all representable shapes and report `IVTS002`.
15. Cross-language metadata: read matching metadata from any .NET assembly; no VB source analyzer.
16. Other generated declarations: do not analyze generated declaration locations, but enforce contracts when user-authored code consumes generated symbols.
17. Type forwarding: rely on Roslyn symbol identity unless tests prove a gap.
18. Native integer aliases: test semantic identity; no syntax-specific handling.
19. Unsupported scenarios: remain silent and document, except cycles and malformed current-source configurations.
20. Cycle scope: only generic-signature propagation graph nodes, not arbitrary recursive object graphs.

Questions requiring approval before implementation:

1. Should `IVTS005` exist, or should malformed lookalike metadata always be ignored unless the compiler itself reports an error?
2. Do you approve nested generic type propagation across containing type parameters as recommended?
3. Should v1 attempt broad semantic use-site coverage including casts, patterns, target-typed `new`, constraints, and tuple element types, or should those be staged after core fields/properties/methods/base lists?

## 20. Sequential implementation tasks

### Task 1: Repository scaffolding

- Objective: Create solution, project skeleton, package metadata, shared build props, analyzer release files, README/CHANGELOG placeholders.
- Affected files: solution, `src/`, `tests/`, `Directory.Build.props`, `Directory.Packages.props`, docs placeholders.
- Approach: Minimal compileable analyzer project targeting `netstandard2.0`; test project targeting current test framework.
- Tests required: `dotnet build`.
- Acceptance criteria: solution builds with no analyzer functionality and no warnings.
- Dependencies: plan approval.
- Risks/open questions: .NET 10 SDK availability in local/CI.

### Task 2: Generator and embedded attributes

- Objective: Generate both attributes with correct namespace, visibility, XML docs, and embedded behavior.
- Affected files: `Generation/`, generator tests.
- Approach: Incremental generator with stable generated source and embedded attribute support.
- Tests required: generator snapshot/semantic tests.
- Acceptance criteria: consuming compilation can use both attributes without runtime reference.
- Dependencies: Task 1.
- Risks/open questions: exact `AddEmbeddedAttributeDefinition()` API behavior.

### Task 3: Diagnostic descriptors and policy extraction

- Objective: Implement diagnostic catalog and parse direct attribute policies.
- Affected files: `Diagnostics/`, `Policies/`.
- Approach: Metadata-name matching, shape validation, immutable policy model.
- Tests required: direct policy extraction and invalid configuration tests.
- Acceptance criteria: valid direct attributes parsed; invalid configs produce proposed diagnostics.
- Dependencies: Task 2 and diagnostic approval.
- Risks/open questions: `IVTS005` approval.

### Task 4: Exact matching

- Objective: Enforce `DisallowExactTypes`.
- Affected files: `Analysis/`, `Policies/`.
- Approach: Semantic identity with dynamic/object normalization and reference nullability erasure.
- Tests required: exact-match matrix.
- Acceptance criteria: exact checks pass, derived types allowed, nullable cases correct.
- Dependencies: Task 3.
- Risks/open questions: type forwarding edge cases.

### Task 5: Assignable matching

- Objective: Enforce `DisallowTypes`.
- Affected files: `Analysis/`.
- Approach: Implement/test definite assignability without user-defined or numeric conversions.
- Tests required: assignable matrix.
- Acceptance criteria: class/interface/variance/array/boxing/ref-like behavior correct; conversions excluded.
- Dependencies: Task 3.
- Risks/open questions: whether `Compilation.ClassifyConversion` is safe enough.

### Task 6: Generic method and method-group use sites

- Objective: Validate explicit and inferred method type arguments, delegate method groups, local functions.
- Affected files: use-site analyzer registrations/tests.
- Approach: Operation analysis for invocation/delegate creation/method references.
- Tests required: generic method matrix.
- Acceptance criteria: inferred and explicit violations diagnosed with useful locations/messages.
- Dependencies: Tasks 4 and 5.
- Risks/open questions: precise locations for inferred arguments.

### Task 7: Broad constructed-type use sites

- Objective: Validate fields, properties, parameters, returns, `typeof`, object creation, base/interface declarations, casts, patterns, defaults, constraints, tuples.
- Affected files: analyzer registrations/tests.
- Approach: Combine operation and targeted syntax-node actions.
- Tests required: use-site matrix.
- Acceptance criteria: no narrow object-creation-only holes for supported v1 locations.
- Dependencies: Tasks 4 and 5.
- Risks/open questions: avoiding duplicate diagnostics across syntax and operation callbacks.

### Task 8: Override and interface member contract propagation

- Objective: Propagate restrictions through overridden and implemented generic members.
- Affected files: `Propagation/`, member analysis tests.
- Approach: Use Roslyn overridden/interface implementation relationships and ordinal mapping.
- Tests required: override/interface/explicit implementation/partial method cases.
- Acceptance criteria: contracts enforce without physical attribute copying.
- Dependencies: Tasks 3, 6.
- Risks/open questions: multiple metadata paths and duplicate suppression.

### Task 9: Generic type inheritance/interface propagation

- Objective: Propagate restrictions through generic base types and interfaces.
- Affected files: `Propagation/`.
- Approach: Build direct mapping from base/interface constructed types to derived generic parameters.
- Tests required: base, interface, multiple interface, transitive, reordering, repeated mapping.
- Acceptance criteria: inherited type contracts are unioned and enforced.
- Dependencies: Task 7.
- Risks/open questions: deterministic conflict/diagnostic precedence.

### Task 10: Signature-based propagation

- Objective: Propagate direct contracts through declaration signatures.
- Affected files: `Propagation/`, symbol signature scanner.
- Approach: Scan declaration signatures, including nested signature types, for direct generic-parameter mappings.
- Tests required: private/static/member signatures, nested `Action<Data<T>>`, no method-body propagation, no transformed type propagation.
- Acceptance criteria: wrappers cannot escape restrictions through direct `Data<T>` signatures.
- Dependencies: Task 9.
- Risks/open questions: nested containing generic parameter behavior approval.

### Task 11: Cycle detection

- Objective: Detect and report cyclic generic-signature propagation graphs.
- Affected files: `Propagation/`.
- Approach: Deterministic graph keys, DFS/SCC detection, single diagnostic per cycle.
- Tests required: simple/long cycles, duplicate suppression, ordinary recursive type allowed.
- Acceptance criteria: no stack overflow and deterministic `IVTS003`.
- Dependencies: Task 10.
- Risks/open questions: overly broad cycle scope.

### Task 12: Cross-assembly enforcement

- Objective: Enforce restrictions from referenced assemblies.
- Affected files: test infrastructure, metadata policy reader.
- Approach: Multi-compilation Roslyn tests and metadata references.
- Tests required: current compilation -> referenced compilation, malformed metadata, generated declarations consumed from user source.
- Acceptance criteria: analyzer does not rely on shared attribute CLR identity.
- Dependencies: Tasks 3-11.
- Risks/open questions: source locations from metadata-only declarations.

### Task 13: Packaging and package-content validation

- Objective: Produce NuGet package with analyzer assets only and no runtime lib.
- Affected files: `.csproj`, packaging props/targets, package tests.
- Approach: Pack and inspect `.nupkg` contents.
- Tests required: package validation tests.
- Acceptance criteria: no `lib/`, analyzer present, README/symbols/source metadata present.
- Dependencies: Tasks 1-12.
- Risks/open questions: analyzer transitivity mechanics.

### Task 14: Transitive enforcement integration tests

- Objective: Prove or document downstream enforcement behavior.
- Affected files: packaging tests, docs.
- Approach: Temporary projects using package/project references.
- Tests required: direct package, project reference, package transitive scenarios.
- Acceptance criteria: supported transitivity claims backed by tests; unsupported scenarios documented.
- Dependencies: Task 13.
- Risks/open questions: NuGet/MSBuild limitations.

### Task 15: Documentation and sample

- Objective: Complete README, docs, diagnostics, limitations, sample.
- Affected files: root docs, `docs/`, `samples/`.
- Approach: Write user-facing examples and runnable valid sample.
- Tests required: sample build, doc links checked manually.
- Acceptance criteria: docs cover required topics and AI-assisted statement is visible.
- Dependencies: diagnostics and behavior stabilized through Task 14.
- Risks/open questions: balancing concise README with complete docs.

### Task 16: CI

- Objective: Add GitHub Actions workflow.
- Affected files: `.github/workflows/ci.yml`.
- Approach: Restore/build/test/pack/validate/sample on .NET 10 with .NET 8 compatibility checks.
- Tests required: local workflow-equivalent commands.
- Acceptance criteria: CI runs release build, tests, pack validation, sample validation; no publish.
- Dependencies: Tasks 13-15.
- Risks/open questions: exact .NET 10 setup action support at implementation time.

### Task 17: Stabilization pass

- Objective: Review diagnostics, public docs, package metadata, performance, and deferred behavior.
- Affected files: all touched files.
- Approach: Run full validation, inspect package, update progress/lessons/deferred docs.
- Tests required: full test suite and package validation.
- Acceptance criteria: release candidate for `0.1.2`.
- Dependencies: all prior tasks.
- Risks/open questions: late behavior changes may require diagnostic catalog review.

## 21. Risks and fallback approaches

- Analyzer false positives: prefer definite-only logic and skip unresolved/error symbols.
- Analyzer false negatives: keep use-site matrix explicit and add regression tests for every discovered gap.
- Duplicate diagnostics: centralize result aggregation by source location and type parameter ordinal.
- Performance: cache per-compilation policy extraction and type relationship results; limit graph traversal.
- Roslyn API mismatch: isolate compiler-version-sensitive logic behind small helper classes and tests.
- Transitive NuGet enforcement: prove with integration tests; if impossible for some scenario, document the exact limitation.
- Embedded attribute identity: rely on metadata contract, not assembly identity.
- Cycle handling too broad: restrict graph nodes to generic-signature propagation only.
- v1 scope creep: keep unsupported scenarios silent and documented unless the spec explicitly requires diagnostics.
