# IvTem.TypeSafety progress log

## Current status

- Phase: Task 17 completed.
- Implementation status: Repository scaffolding created; embedded attribute generator implemented and tested; diagnostic catalog and direct policy extraction implemented and tested; exact direct constructed-type matching implemented and tested; assignable direct constructed-type matching implemented and tested; generic method invocation, inference, method-group, delegate conversion, and generic local-function use sites implemented and tested; broad constructed generic type use sites implemented and tested; override and interface member contract propagation implemented and tested; generic type inheritance and interface propagation implemented and tested; signature-based named type propagation implemented and tested; cyclic generic-signature propagation detection implemented and tested; cross-assembly metadata enforcement implemented and tested; analyzer-only NuGet package layout implemented and package-content validation automated; NuGet/MSBuild direct, project-reference, and package-transitive enforcement proved by integration tests; user-facing documentation and runnable sample added; GitHub Actions-compatible CI added for restore, Release build, tests, package creation, package artifact upload, sample build, and limited .NET 8 SDK compatibility inspection; final v0.1.0 stabilization pass completed with release tracking, changelog, decisions, package shape, and full validation refreshed.
- Stop condition: Task 17 is complete. Wait for explicit release, commit, or follow-up instruction.
- Post-task release support: A manually triggered nuget.org Trusted Publishing workflow has been added; publishing still requires a matching nuget.org policy and GitHub environment configuration.

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
- Task 7: Ran `dotnet test IvTem.TypeSafety.slnx --filter BroadConstructedTypeUseSiteTests`; 12 broad constructed-type use-site tests passed.
- Task 7: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 64 tests.
- Task 7: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 8: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 8: Ran `dotnet test IvTem.TypeSafety.slnx --filter MemberPropagation`; 7 member-propagation tests passed.
- Task 8: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 71 tests.
- Task 8: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 9: Ran `dotnet test IvTem.TypeSafety.slnx --filter TypePropagation`; 10 type-propagation tests passed.
- Task 9: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 81 tests.
- Task 9: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 10: Ran `dotnet test IvTem.TypeSafety.slnx --filter SignaturePropagation`; 9 signature-propagation tests passed.
- Task 10: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 90 tests.
- Task 10: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 11: Ran `dotnet test IvTem.TypeSafety.slnx --filter Cycles`; 7 cycle-detection tests passed.
- Task 11: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 97 tests.
- Task 11: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 12: Ran `dotnet test IvTem.TypeSafety.slnx --filter CrossAssembly`; 7 cross-assembly tests passed.
- Task 12: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 104 tests.
- Task 12: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors.
- Task 13: Ran `dotnet pack IvTem.TypeSafety.slnx -c Release`; Release `.nupkg` and `.snupkg` artifacts were produced.
- Task 13: Ran `dotnet test IvTem.TypeSafety.slnx --filter PackageContentTests`; 1 package-content validation test passed.
- Task 13: Ran `dotnet build IvTem.TypeSafety.slnx -c Release`; build succeeded with 0 warnings and 0 errors.
- Task 13: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 105 tests.
- Task 13: Inspected `IvTem.TypeSafety.0.1.0.nupkg`; it contains `analyzers/dotnet/cs/IvTem.TypeSafety.dll`, `README.md`, and nuspec/package metadata with no `lib/` entries.
- Task 13: Inspected `IvTem.TypeSafety.0.1.0.snupkg`; it contains `analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.pdb` with no `lib/` entries.
- Task 14: Ran `dotnet restore IvTem.TypeSafety.slnx`; restore succeeded after elevated access to local SDK metadata under the user profile.
- Task 14: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; build succeeded with 0 warnings and 0 errors after aligning Roslyn package references to the SDK compiler.
- Task 14: Ran `dotnet test IvTem.TypeSafety.slnx --filter "FullyQualifiedName~Packaging"`; 4 packaging/transitive integration tests passed.
- Task 14: Ran `dotnet test IvTem.TypeSafety.slnx`; full test suite passed with 108 tests.
- Task 14: Ran `dotnet pack IvTem.TypeSafety.slnx -c Release`; Release `.nupkg` and `.snupkg` artifacts were produced after elevated access to local SDK metadata under the user profile.
- Task 14: Inspected `IvTem.TypeSafety.0.1.0.nupkg`; it contains `analyzers/dotnet/cs/IvTem.TypeSafety.dll`, `buildTransitive/IvTem.TypeSafety.props`, `README.md`, and nuspec/package metadata with no `lib/` entries.
- Task 15: Ran `dotnet build samples/IvTem.TypeSafety.Sample`; sample build succeeded with 0 warnings and 0 errors.
- Task 15: Ran `dotnet build IvTem.TypeSafety.slnx --no-restore`; solution build succeeded with 0 warnings and 0 errors after adding the sample project.
- Task 15: Ran `dotnet test IvTem.TypeSafety.slnx --filter "FullyQualifiedName~DirectRestrictionPolicyExtractionTests|FullyQualifiedName~CycleDetectionTests"`; docs-sensitive diagnostic tests passed.
- Task 15: Ran `dotnet test IvTem.TypeSafety.slnx --no-build`; full test suite passed with 108 tests.
- Task 15: Ran manual path checks for README documentation links and sample references.
- Task 16: Ran `dotnet restore IvTem.TypeSafety.slnx`; restore succeeded after elevated access to local SDK metadata under the user profile.
- Task 16: Ran `dotnet build IvTem.TypeSafety.slnx -c Release --no-restore`; Release build succeeded with 0 warnings and 0 errors.
- Task 16: Ran `dotnet test IvTem.TypeSafety.slnx -c Release --no-build`; full Release test suite passed with 108 tests.
- Task 16: Ran `dotnet pack src/IvTem.TypeSafety/IvTem.TypeSafety.csproj -c Release --no-restore`; package creation succeeded after elevated access to local SDK metadata under the user profile.
- Task 16: Ran `dotnet build samples/IvTem.TypeSafety.Sample/IvTem.TypeSafety.Sample.csproj -c Release --no-restore`; sample Release build succeeded with 0 warnings and 0 errors.
- Task 16: Ran a local temporary .NET 8 SDK check with `global.json` selecting SDK `8.0.414`; `dotnet msbuild ... -getProperty:TargetFramework` returned `netstandard2.0` and analyzer project restore succeeded.
- Task 16: Inspected `.github/workflows/ci.yml` for obvious YAML and command issues.
- Task 17: Ran `dotnet restore IvTem.TypeSafety.slnx`; initial sandboxed run failed with `MSB4184` because access to `C:\Users\Ivan Temelkov\AppData\Local\Microsoft SDKs` was denied, then elevated restore succeeded.
- Task 17: Ran `dotnet build IvTem.TypeSafety.slnx -c Release --no-restore`; a first parallel run collided with `dotnet test` and failed because `testhost` locked the analyzer DLL, then the serial rerun succeeded with 0 warnings and 0 errors.
- Task 17: Ran `dotnet test IvTem.TypeSafety.slnx -c Release --no-build`; full Release test suite passed with 108 tests.
- Task 17: Ran `dotnet pack src/IvTem.TypeSafety/IvTem.TypeSafety.csproj -c Release --no-restore`; initial sandboxed run hit the local SDK metadata access issue, then elevated project pack succeeded.
- Task 17: Ran `dotnet build samples/IvTem.TypeSafety.Sample/IvTem.TypeSafety.Sample.csproj -c Release --no-restore`; sample Release build succeeded with 0 warnings and 0 errors.
- Task 17: Ran `dotnet pack -c Release`; found that the solution-level pack attempted to package the runnable sample and failed with `NU5039` because inherited package metadata referenced `README.md`.
- Task 17: Inspected diagnostic descriptors, README, diagnostics, architecture, limitations, package metadata, package contents, deferred-feature documentation, and analyzer startup/cache paths.
- Task 17: After marking the sample non-packable, reran `dotnet restore IvTem.TypeSafety.slnx`; restore succeeded.
- Task 17: Reran `dotnet build IvTem.TypeSafety.slnx -c Release --no-restore`; Release build succeeded with 0 warnings and 0 errors.
- Task 17: Reran `dotnet test IvTem.TypeSafety.slnx -c Release --no-build`; full Release test suite passed with 108 tests.
- Task 17: Reran `dotnet pack -c Release`; solution-level Release pack succeeded and produced the analyzer package.
- Task 17: Ran `dotnet test IvTem.TypeSafety.slnx -c Release --no-build --filter PackageContentTests`; package-content validation passed with 1 test.
- Task 17: Reran `dotnet build samples/IvTem.TypeSafety.Sample/IvTem.TypeSafety.Sample.csproj -c Release --no-restore`; sample Release build succeeded with 0 warnings and 0 errors.
- Task 17: Manually inspected `IvTem.TypeSafety.0.1.0.nupkg`; it contains `analyzers/dotnet/cs/IvTem.TypeSafety.dll`, `buildTransitive/IvTem.TypeSafety.props`, and `README.md`, with no `lib/` entries.
- Task 17: Manually inspected `IvTem.TypeSafety.0.1.0.snupkg`; it contains `analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.pdb`, with no `lib/` entries.
- Post-task release support: Inspected `.github/workflows/publish-nuget.yml` for obvious YAML and command issues.
- Post-task release support: Replaced the long-lived `NUGET_API_KEY` secret path with NuGet Trusted Publishing through `NuGet/login@v1` and GitHub OIDC.
- Post-task release support: Ran `python -c "import yaml, pathlib; yaml.safe_load(pathlib.Path('.github/workflows/publish-nuget.yml').read_text()); print('YAML parsed')"`; the workflow YAML parsed successfully.
- Post-task release support: Ran `dotnet msbuild src/IvTem.TypeSafety/IvTem.TypeSafety.csproj -getProperty:Version` outside the sandbox after the known SDK metadata access issue; it returned `0.1.0`.
- Post-task release support: Ran `dotnet msbuild src/IvTem.TypeSafety/IvTem.TypeSafety.csproj -getProperty:TargetFramework` outside the sandbox after the known SDK metadata access issue; it returned `netstandard2.0`.

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
- Task 7: Added reusable constructed generic type validation for `INamedTypeSymbol` use sites, sharing exact and assignable policy matching.
- Task 7: Added source-span plus generic-argument ordinal diagnostic de-duplication across syntax and operation callbacks.
- Task 7: Added operation coverage for object creation, `typeof`, and collection expressions, including target-typed locations where explicit type-argument syntax is unavailable.
- Task 7: Preserved generic method use-site enforcement through the shared validator so Task 6 behavior continues to use the same matching and de-duplication path.
- Task 7: Suppressed alias declarations and unresolved/error constructed type arguments for the broad use-site pass.
- Task 7: Added broad use-site tests covering fields, properties, parameters, return types, `typeof`, object creation, target-typed `new`, base/interface declarations, casts, `is`, `as`, `default(...)`, attribute `typeof(...)`, constraints, tuple element types, collection expressions, aliases, broken code, and syntax/operation de-duplication.
- Task 8: Added `MemberRestrictionPolicyProvider` to collect generic method contracts from direct declarations, partial method counterpart symbols, overridden methods, explicit interface implementations, and Roslyn-resolved implicit interface implementations.
- Task 8: Updated generic method use-site validation to use unioned member policies while leaving named generic type policy extraction direct-only for Task 9.
- Task 8: Added member propagation tests covering implicit interface implementation, overrides, explicit interface implementation, multiple interface contract unioning, ordinal mapping across multiple method type parameters, partial method declaration/implementation behavior, and duplicate inherited interface paths.
- Task 9: Added `NamedTypeRestrictionPolicyProvider` to collect generic named-type contracts from direct declarations plus generic base class and interface mappings.
- Task 9: Updated named generic type use-site validation to enforce unioned direct and inherited type-parameter policies while preserving the existing member-policy path for generic methods.
- Task 9: Added type-propagation tests covering generic interfaces, generic base classes, multiple inherited contracts, transitive inheritance, partial declaration contract merging, reordered mappings, repeated mappings, immediate concrete base/interface declaration violations, and diamond-path diagnostic de-duplication.
- Task 10: Extended `NamedTypeRestrictionPolicyProvider` to scan named type declaration signatures for direct generic-parameter mappings.
- Task 10: Added signature scanning for fields, properties, events, method return types, method parameters, method generic constraints, and named type generic constraints.
- Task 10: Added recursive traversal through signature containers so nested signatures such as `Action<Data<T>>` propagate while transformed restricted arguments such as `Data<List<T>>`, `Data<T[]>`, `Data<(T, string)>`, and `Data<Wrapper<T>>` remain unsupported.
- Task 10: Added signature-propagation tests covering field, property, method return and parameter, event, private static member, generic constraint, method-body local exclusion, transformed argument exclusion, and nested containing-type behavior.
- Task 11: Added graph-based cycle detection for named-type generic-signature propagation nodes keyed by original type definition plus type-parameter ordinal.
- Task 11: Added deterministic strongly connected component analysis and one `IVTS003` report per cycle component.
- Task 11: Added deterministic cycle diagnostic locations using ordered source type-parameter declarations.
- Task 11: Suppressed propagated use-site diagnostics for cyclic source graph nodes while preserving direct policy extraction.
- Task 11: Added cycle-detection tests covering two-type cycles, longer cycles, duplicate paths, nongeneric recursion exclusion, stack-safety, acyclic deep propagation, and suppression of cyclic-propagation use-site cascades.
- Task 12: Added analyzer test infrastructure for in-memory referenced assemblies with generated embedded attributes and additional metadata references.
- Task 12: Added defensive skipping for unresolved/error forbidden metadata types while preserving existing invalid configuration diagnostics for open/unbound generics.
- Task 12: Added cross-assembly tests proving referenced generic type contracts, embedded attribute assembly-identity independence, referenced generic method contracts, referenced propagated type contracts, metadata-only malformed lookalike handling, metadata-only invalid configurations without source locations, and generated referenced declarations consumed from metadata.
- Task 13: Configured analyzer-only package output for `IvTem.TypeSafety` version `0.1.0` with MIT license metadata, README inclusion, Git repository metadata, Source Link package reference, and `.snupkg` symbol package output.
- Task 13: Added explicit package content under `analyzers/dotnet/cs/` and suppressed normal runtime `lib/` output from the main package.
- Task 13: Added package-content validation that runs `dotnet pack`, opens the produced `.nupkg` and `.snupkg`, and asserts required analyzer assets, README, metadata, repository commit metadata, symbol package output, Source Link payload, and absence of `lib/` entries.
- Task 13: Updated README and changelog to describe the implemented analyzer/source-generator and analyzer-only package shape.
- Task 14: Added `buildTransitive/IvTem.TypeSafety.props` to the package so downstream projects that receive `IvTem.TypeSafety` transitively also receive the analyzer/source-generator asset.
- Task 14: Added temporary-project integration tests for direct `PackageReference`, project-reference transitive flow, and package-reference transitive flow.
- Task 14: Serialized package-build tests and isolated temporary NuGet global package folders to avoid Release pack races and stale local package cache reuse.
- Task 14: Downgraded the Roslyn package reference from `5.9.0` to `5.6.0` because the packaged analyzer must load in the installed .NET SDK compiler used by real builds.
- Task 14: Added `docs/architecture.md` and `docs/limitations.md` to document the tested NuGet/MSBuild enforcement boundary.
- Task 15: Expanded README with visible AI-assisted/OpenAI Codex statement, installation, examples, diagnostics summary, limitations, and package transitivity notes.
- Task 15: Expanded architecture documentation with generator/analyzer split, policy model, matching behavior, propagation, cycle handling, cross-assembly metadata, and NuGet/MSBuild flow.
- Task 15: Added `docs/diagnostics.md` with documentation for `IVTS001` through `IVTS005`.
- Task 15: Expanded limitations documentation with deferred analysis, configuration boundaries, cycle handling, and analyzer activation limits.
- Task 15: Added `samples/IvTem.TypeSafety.Sample` with valid usage for both attributes and intentionally invalid examples in a non-compiled `.cs.txt` file.
- Task 15: Added the sample project to `IvTem.TypeSafety.slnx`.
- Task 16: Added `.github/workflows/ci.yml` with pull request and `main` push triggers.
- Task 16: Added a primary CI job that checks out full Git history, installs .NET 10 and .NET 8 SDKs, restores the solution, builds Release, runs Release tests, packs the analyzer package, builds the sample, and uploads package/test artifacts without publishing to NuGet.
- Task 16: Added a secondary .NET 8 SDK compatibility job that pins SDK 8 in a temporary runner directory, verifies SDK selection, confirms the analyzer project target framework is `netstandard2.0`, and restores the analyzer project.
- Task 17: Marked the runnable sample project as non-packable so solution-level Release packing produces only the analyzer/source-generator package.
- Task 17: Moved the `0.1.0` diagnostic catalog from unshipped to shipped release tracking, preserving `IVTS004` as a reserved descriptor.
- Task 17: Updated the changelog with broad use-site coverage and solution-level pack stabilization.
- Task 17: Updated the decision log with release-candidate stabilization decisions.
- Post-task release support: Added `.github/workflows/publish-nuget.yml` for manually triggered nuget.org Trusted Publishing.

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

## Decisions made during Task 7

- Kept broad constructed-type detection mostly syntax-driven for explicit generic type syntax because Roslyn binds those locations reliably across declarations, patterns, casts, constraints, tuple elements, `default(...)`, and attribute arguments.
- Added operation analysis only where it materially extends or overlaps explicit syntax coverage: object creation, `typeof`, and collection expressions.
- Used the explicit type-argument location whenever available; operation-only target-typed use sites fall back to the expression location such as `new()` or `[]`.
- Suppressed using-alias declarations for v1 because alias-specific enforcement remains unsupported and alias uses do not expose the constructed generic syntax directly.
- Allowed distinct diagnostics for distinct semantic use sites in the same statement, such as an invalid return type plus a target-typed `new()` expression; de-duplication only suppresses overlapping callbacks for the same source span and generic argument ordinal.

## Decisions made during Task 8

- Scoped propagation to generic methods because C# has generic methods but not generic properties/events/operators in the same use-site shape; generic type inheritance remains Task 9.
- Used Roslyn's `FindImplementationForInterfaceMember` for implicit interface contracts and `ExplicitInterfaceImplementations` for explicit contracts instead of name/signature text matching.
- Mapped inherited member type parameters to implementation type parameters by ordinal, matching C# override/interface generic arity rules.
- Combined direct and inherited restrictions as a semantic union per restriction kind, preserving one `IVTS001` per offending method type argument.
- Included partial method definition and implementation counterpart symbols as contract sources so attributes on either part are visible at use sites.

## Decisions made during Task 9

- Used a per-compilation named-type policy provider keyed by original type definition so constructed use sites reuse the same inherited contract calculation.
- Mapped base/interface policies only when the constructed base/interface type argument is directly the derived type parameter in scope; transformed mappings such as wrappers, arrays, and nested constructed arguments remain deferred.
- Walked generic base classes and `AllInterfaces` so direct, transitive, and multiple-interface paths contribute to the derived type contract.
- Deduplicated inherited forbidden types per policy kind by semantic type identity so diamond paths and repeated mappings preserve one diagnostic per offending generic argument.
- Kept immediate concrete forbidden base/interface declaration diagnostics in the existing constructed-type use-site path rather than adding a separate declaration analyzer.

## Decisions made during Task 10

- Reused `NamedTypeRestrictionPolicyProvider` for signature propagation so direct, inherited, and signature-derived named-type contracts are unioned before use-site validation.
- Scanned declarations semantically from symbols instead of syntax so private/public and static/instance members participate consistently across fields, properties, events, methods, and constraints.
- Recursed through generic signature containers only to find candidate constructed restricted types; propagation edges are still created only when a restricted generic argument is exactly an in-scope type parameter of the type being analyzed.
- Treated nested containing type parameters as out of scope for the nested type's own propagated contract in v1; only generic parameters declared by the analyzed named type can receive signature-derived policies.
- Left method-body locals and transformed generic arguments out of propagation.

## Decisions made during Task 11

- Modeled cycle detection as a graph separate from policy extraction, with nodes `(original generic type definition, type-parameter ordinal)` and edges only for direct generic-parameter mappings already supported by propagation.
- Used strongly connected component detection rather than fixed-point reasoning; v1 reports cyclic components as `IVTS003`.
- Reported each component once per compilation using a deterministic key built from ordered node display strings.
- Chose the diagnostic location from the earliest deterministic source type-parameter declaration in the component, falling back to `Location.None` only for metadata-only participants.
- Suppressed only propagated policies from cyclic source nodes, rather than suppressing an entire generic type, so unrelated noncyclic ordinals and direct restrictions can still participate.

## Decisions made during Task 12

- Treated metadata-only malformed lookalike attributes as ignored defensively, preserving `IVTS005` for current-source malformed lookalikes only.
- Kept attribute identity based on fully qualified metadata name plus expected shape rather than shared assembly identity.
- Proved cross-assembly behavior with C#-emitted metadata references; the analyzer reads standard CLR metadata and does not add language-specific source analysis for non-C# assemblies.

## Decisions made during Task 13

- Kept `IncludeBuildOutput=true` only to let NuGet produce `.snupkg` symbols, then removed collected runtime build output before packaging so the main `.nupkg` has no `lib/` assembly.
- Added the analyzer DLL as explicit TFM-specific package content at `analyzers/dotnet/cs/IvTem.TypeSafety.dll` to avoid NuGet inserting `netstandard2.0` under the analyzer path in the main package.
- Scoped `NU5128` suppression to the analyzer project because the no-`lib/` package shape is intentional for analyzer/source-generator distribution and is covered by package-content tests.
- Accepted NuGet's deterministic symbol package path `analyzers/dotnet/cs/netstandard2.0/IvTem.TypeSafety.pdb` while requiring no `lib/` entries in the `.snupkg`.

## Decisions made during Task 14

- Added a `buildTransitive` props file rather than relying on NuGet analyzer assets alone, because transitive consumers need an imported MSBuild asset that explicitly adds the analyzer.
- Treat normal package dependency flow as the supported transitive boundary; dependencies hidden with `PrivateAssets="all"` or equivalent asset exclusions are not claimed.
- Kept Roslyn analyzer dependencies no newer than the SDK compiler version proven by integration tests to avoid `CS9057` analyzer load failures.

## Decisions made during Task 15

- Documented `IVTS004` as reserved because the descriptor exists but no current analyzer path emits it.
- Kept invalid sample examples in `InvalidExamples.cs.txt` so the sample remains runnable and CI-safe.
- Referenced the local analyzer project as an analyzer in the sample rather than consuming a packed local NuGet package, keeping sample validation independent from Release pack output.

## Decisions made during Task 16

- Did not add `global.json` to the repository; CI installs explicit SDK channels while local checkouts remain free to use the installed .NET 10 SDK selected by normal roll-forward behavior.
- Used a single primary CI job for restore, build, tests, package creation, package artifact upload, and sample build because the current suite already serializes package-build tests where needed.
- Kept `dotnet pack` without `--no-build` because the package-content target depends on `Build`; `--no-build` fails with `NETSDK1085` for this analyzer package shape.
- Limited the .NET 8 SDK job to target-framework inspection and restore. A full SDK 8 analyzer build currently fails with `CS9057` because the Roslyn analyzer-rule dependency requires a newer compiler than SDK 8 provides.

## Decisions made during Task 17

- Treated `IVTS001`, `IVTS002`, `IVTS003`, and `IVTS005` as the emitted `0.1.0` diagnostic set, while keeping `IVTS004` in the shipped catalog as a reserved non-emitted descriptor.
- Kept the sample project in the solution but marked it non-packable, because the repository-level `dotnet pack -c Release` command should produce the analyzer package without trying to package runnable examples.
- Did not add late analyzer behavior or a new performance benchmark during stabilization; the release candidate relies on the existing 108-test suite, compilation-start analyzer state, and documented unsupported scenarios.

## Decisions made during NuGet publish workflow

- Kept publishing separate from CI so normal `push` and `pull_request` workflows continue to build, test, pack, and upload artifacts without publishing.
- Required manual `package_version` input to match the project `Version`, required the workflow to run from `main`, and used the `nuget.org` GitHub environment as an optional approval gate.
- Required manual `nuget_user` input for the nuget.org profile name and used `NuGet/login@v1` with `id-token: write` to exchange GitHub OIDC for a temporary NuGet publish token.
- Published the `.nupkg` from the package output directory so the colocated `.snupkg` can be discovered and uploaded by NuGet, using `--skip-duplicate` for rerun tolerance.

## Unresolved issues

- Whether `IVTS005` should remain enabled for all current-source malformed lookalike metadata or be narrowed after review.
- Whether broad semantic use-site coverage should be implemented in one task set or staged after core use sites.
- No current unresolved NuGet analyzer transitivity issue remains for the tested normal dependency-flow scenarios.
- No synthetic performance benchmark exists yet; performance-sensitive propagation paths use per-compilation caches, but larger real-world solutions should be monitored after initial release.

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
- Real SDK package-load tests require the analyzer to reference Roslyn `5.6.0` or older on this machine; Roslyn `5.9.0` compiled but failed as a packaged analyzer with `CS9057` because the SDK compiler was older.
- Analyzer projects targeting `netstandard2.0` should avoid production positional records unless an `IsExternalInit` compatibility shim is intentionally added.
- `System.Attribute` and `System.Type` are not represented by `SpecialType` enum values in Roslyn; metadata-name checks are needed for those shape validations.
- Roslyn exposes `[DisallowTypes(null)]` as a null params array and `[DisallowTypes(typeof(string), null)]` as an array containing a null entry, so both cases can be diagnosed distinctly.
- Running solution build and tests in parallel can lock shared analyzer outputs on Windows; Task 17 validation should be run serially for reliable release evidence.
- Sample projects that inherit repository package metadata should explicitly set `<IsPackable>false</IsPackable>` when they are included in the solution.
