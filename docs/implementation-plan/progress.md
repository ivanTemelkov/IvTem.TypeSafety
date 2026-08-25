# IvTem.TypeSafety progress log

## Current status

- Phase: Task 6 completed.
- Implementation status: Repository scaffolding created; embedded attribute generator implemented and tested; diagnostic catalog and direct policy extraction implemented and tested; exact direct constructed-type matching implemented and tested; assignable direct constructed-type matching implemented and tested; generic method invocation, inference, method-group, delegate conversion, and generic local-function use sites implemented and tested.
- Stop condition: Wait for explicit instruction before Task 7.

## Validation performed

- Read `Instructions.md`.
- Read `CODING-STYLE.md`.
- Checked `docs/agent/`; no referenced convention files currently exist there.
- Inspected repository files.
- Inspected Git branch, recent commits, and remote metadata.
- Task 1: Confirmed installed SDKs with `dotnet --info`; .NET SDK `10.0.301` is available.
- Task 1: Confirmed Git remote as `https://github.com/ivanTemelkov/IvTem.TypeSafety.git`.
- Task 1: Ran `dotnet restore IvTem.TypeSafety.slnx`; restore succeeded after removing an unavailable direct verifier package.
- Task 1: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 1: Ran `git status --short`; scaffolding files are untracked, and pre-existing `docs/` content remains untracked.
- Task 2: Ran `dotnet test IvTem.TypeSafety.slnx --filter TypeSafetyAttributeGeneratorTests`; 6 generator tests passed.
- Task 2: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 3: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded after replacing positional records with `netstandard2.0`-portable immutable classes and correcting Roslyn metadata checks.
- Task 3: Ran `dotnet test IvTem.TypeSafety.slnx --filter DirectRestrictionPolicyExtractionTests`; 14 analyzer/policy extraction tests passed.
- Task 4: Ran `dotnet test IvTem.TypeSafety.slnx --filter ExactMatching`; 7 exact-matching tests passed.
- Task 4: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed.
- Task 4: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 5: Ran `dotnet test IvTem.TypeSafety.slnx --filter AssignableMatching`; 16 assignable-matching tests passed.
- Task 5: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 43 tests.
- Task 5: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 6: Ran `dotnet test IvTem.TypeSafety.slnx --filter GenericMethodUseSiteTests`; 9 generic-method use-site tests passed.
- Task 6: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 52 tests.
- Task 6: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.

## Work completed

- Created persistent implementation plan in `docs/implementation-plan/implementation-plan.md`.
- Created this progress log.
- Created architectural decision log in `docs/implementation-plan/decisions.md`.
- Kept `docs/implementation-plan/implementation-plan.md` as the high-level outline and added detailed task files under `docs/implementation-plan/tasks/`.
- Task 1: Created `IvTem.TypeSafety.slnx` with `src` and `tests` solution folders.
- Task 1: Created `Directory.Build.props` with nullable, warnings-as-errors, deterministic build, repository, and package metadata.
- Task 1: Created `Directory.Packages.props` with central package management.
- Task 1: Created `src/IvTem.TypeSafety/IvTem.TypeSafety.csproj` targeting `netstandard2.0`.
- Task 1: Created `tests/IvTem.TypeSafety.Tests/IvTem.TypeSafety.Tests.csproj` targeting `net10.0` with xUnit and Roslyn testing dependencies.
- Task 1: Added placeholder analyzer release files, changelog, README, and source/test ownership folders.
- Task 2: Added `TypeSafetyAttributeGenerator` as an incremental source generator.
- Task 2: Added deterministic generated source for `IvTem.TypeSafety.DisallowTypesAttribute` and `IvTem.TypeSafety.DisallowExactTypesAttribute`.
- Task 2: Added generator tests covering source emission, generic-parameter usage, internal accessibility, repeated usage, namespace and constructor shape, and absence of a consumer runtime reference to the analyzer assembly.
- Task 3: Added centralized diagnostic descriptors for `IVTS001` through `IVTS005`.
- Task 3: Added `TypeSafetyAnalyzer` with declaration-time analysis for named type and method generic parameters.
- Task 3: Added direct policy extraction for `DisallowTypesAttribute` and `DisallowExactTypesAttribute` based on namespace plus metadata name, expected shape validation, immutable policy output, and semantic de-duplication per restriction kind.
- Task 3: Added configuration diagnostics for empty type lists, null type lists, null entries, open/unbound generic types, forbidden types containing generic parameters, and `DisallowTypes(typeof(object))`.
- Task 3: Added malformed lookalike metadata diagnostics for current-source attributes that use the owned metadata names but fail the v1 shape contract.
- Task 3: Added analyzer test infrastructure that runs the embedded attribute generator before analyzer diagnostics.
- Task 3: Added focused policy/configuration tests covering valid direct extraction, exact extraction, multiple attributes, multiple constructor arguments, duplicate de-duplication, invalid configuration cases, malformed metadata, and `DisallowExactTypes(typeof(object))`.
- Task 4: Added exact type matching with `dynamic` normalized to `System.Object`, nullable reference annotations erased via semantic comparison, and nullable value types preserved as distinct constructed types.
- Task 4: Added explicit generic type syntax analysis for direct constructed type uses and `IVTS001` reporting at offending type argument locations.
- Task 4: Added exact matching tests for exact class rejection, derived class allowance, nullable reference annotations, `dynamic`, nullable value types, generic parameter constraints, and alias/framework type-name semantic identity.
- Task 5: Added assignable type matching for `DisallowTypesAttribute` using filtered implicit identity/reference/boxing conversions plus an explicit implemented-interface fallback for ref-like type interface relationships.
- Task 5: Extended constructed generic type syntax enforcement to aggregate `DisallowTypes` and `DisallowExactTypes` matches into one `IVTS001` per offending generic argument.
- Task 5: Added assignable matching tests for same type, derived class, interface implementation, generic variance, array covariance, value-type boxing, value-type interface boxing, ref-like interface implementation, user-defined conversion rejection, numeric conversion rejection, nested type argument non-propagation, direct generic constraints, deferred constraint chains, and diagnostic aggregation.
- Task 6: Added operation analysis for generic method invocations, delegate creation, and delegate-conversion method groups.
- Task 6: Added constructed `IMethodSymbol` validation after type inference, mapping method type parameters to type arguments by ordinal and reusing the existing direct-policy matchers.
- Task 6: Added fallback method-use diagnostic locations for inferred arguments and explicit type-argument locations when method generic syntax is available.
- Task 6: Added generic method tests covering explicit violations, inferred violations and allowances, method groups, generic local functions, diagnostic aggregation, explicit argument location, inferred-type message content, and delegate-conversion de-duplication.

## Decisions made during planning

- Plan proposes one analyzer/source-generator project packaged only as analyzer assets.
- Plan proposes no runtime `lib/` assets.
- Plan proposes stable diagnostic IDs `IVTS001` through `IVTS004`, with `IVTS005` left as a review question.
- Plan recommends broad v1 use-site coverage but leaves staging as an approval question.

## Decisions made during Task 1

- Used `.slnx` because the installed .NET 10 SDK supports it by default.
- Used the actual Git remote for repository and package metadata.
- Kept implicit usings disabled solution-wide to favor explicit imports for analyzer/source-generator portability.
- Configured initial analyzer packing shape with build output under `analyzers/dotnet/cs/` and no normal build output packing; package content validation remains deferred to Task 13.
- Removed direct `Microsoft.CodeAnalysis.Testing.Verifiers.XUnit` usage because NuGet did not contain version `1.1.4`; the concrete verifier/harness choice is deferred until real tests are added.
- Task 2 uses a direct `CSharpGeneratorDriver` test harness instead of Roslyn testing framework wrappers, because the direct harness is sufficient for the embedded-source behavior and avoids stale Roslyn testing package conflicts.
- The test project references `IvTem.TypeSafety` as a normal private project reference so tests can instantiate the generator; consumer-reference behavior is proven inside the generated consumer compilation.

## Decisions made during Task 3

- Kept policy extraction independent from use-site enforcement; valid policies are parsed and tested, but `IVTS001` matching remains deferred to Tasks 4 and 5.
- Implemented `IVTS005` for current-source malformed lookalike attributes with the owned metadata names; referenced metadata-only behavior remains deferred to Task 12.
- Used namespace plus metadata name for attribute recognition, not assembly identity, because generated attributes are embedded into consuming compilations.
- Required the v1 attribute constructor shape to be one `params System.Type[]` parameter.
- Used `SymbolEqualityComparer.Default` for semantic duplicate removal per restriction kind, preserving first declaration order with display name as a deterministic tie breaker.
- Used immutable sealed classes instead of positional records in production policy models because the analyzer assembly targets `netstandard2.0`.

## Decisions made during Task 4

- Limited use-site enforcement to explicit constructed generic type syntax so generic method inference and broader semantic locations remain staged for Tasks 06 and 07.
- Reused direct policy extraction for use-site matching but suppressed configuration diagnostics in the use-site pass to avoid duplicate `IVTS002`/`IVTS005` reports; declaration analysis remains responsible for configuration diagnostics.
- Used `SymbolEqualityComparer.Default` after `dynamic` normalization for exact matching, which erases nullable reference annotations while keeping `Nullable<T>` distinct from `T`.
- Skipped unresolved/error type symbols in use-site matching to avoid analyzer noise on incomplete or uncompilable code.

## Decisions made during Task 5

- Used `Compilation.ClassifyConversion(actual, forbidden)` only when the conversion is implicit identity, implicit reference, or implicit boxing, so numeric, nullable, dynamic, and user-defined conversions are not treated as assignability.
- Added explicit interface walking for implemented interfaces because ref-like types that implement interfaces are not covered by the filtered reference/boxing conversion path.
- Kept generic type parameter reasoning limited to direct non-type-parameter class/interface constraints; chained constraints such as `where T : U where U : Exception` remain intentionally deferred.
- Preserved Task 4's explicit constructed generic type syntax boundary; assignable matching does not add method inference, broad use-site coverage, or nested type-argument propagation.

## Decisions made during Task 6

- Used Roslyn operation analysis for method use sites because it exposes constructed generic method symbols after type inference.
- Registered invocation, delegate creation, and conversion operations, then normalized each relevant operation to one constructed `IMethodSymbol`.
- Suppressed conversion analysis when it is nested under another conversion or delegate creation operation to avoid duplicate method-group diagnostics.
- Kept generic lambda support deferred; Task 6 covers named generic methods and generic local functions exposed as `IMethodSymbol`.
- Reused `IVTS001` for method type-argument violations because the existing descriptor already includes the offending actual type and generic parameter.

## Unresolved issues

- Whether `IVTS005` should remain enabled for all current-source malformed lookalike metadata or be narrowed after review.
- Whether nested generic type propagation should cross containing type parameters in v1.
- Whether broad semantic use-site coverage should be implemented in one task set or staged after core use sites.
- NuGet analyzer transitivity must be proven by integration tests before documentation claims it.
- Source Link and package contents have not been validated beyond build/restore; Task 13 must inspect the produced `.nupkg`.
- Cross-assembly behavior for malformed metadata-only lookalikes remains unimplemented until Task 12.

## Deferred features from specification

- Code fixes.
- Type alias-specific enforcement.
- Reflection/data-flow generic creation analysis.
- XML documentation `cref` enforcement.
- Method-body contract propagation.
- Nested symbolic propagation through transformed type arguments.
- Generic constraint chain reasoning.
- Special constraint reasoning for `struct`, `unmanaged`, `class`, and `notnull`.
- Correct fixed-point cycle support; v1 reports cycles as errors.

## Lessons learned

- The repository is intentionally at a planning/bootstrap stage.
- The implementation risk is concentrated in semantic coverage, propagation, de-duplication, and package transitivity rather than source generation.
- The style guide conflicts mildly with the `netstandard2.0` analyzer target; the plan separates repository tooling from analyzer assembly target framework.
- The task outline is easier to preserve if each task has a separate execution contract file.
- `dotnet sln` and `dotnet restore` need access to local SDK metadata under the user profile on this machine.
- Current Roslyn compiler package metadata confirms `Microsoft.CodeAnalysis.CSharp` `5.9.0` supports `netstandard2.0` and .NET 10.
- Analyzer projects targeting `netstandard2.0` should avoid production positional records unless an `IsExternalInit` compatibility shim is intentionally added.
- `System.Attribute` and `System.Type` are not represented by `SpecialType` enum values in Roslyn; metadata-name checks are needed for those shape validations.
- Roslyn exposes `[DisallowTypes(null)]` as a null params array and `[DisallowTypes(typeof(string), null)]` as an array containing a null entry, so both cases can be diagnosed distinctly.
