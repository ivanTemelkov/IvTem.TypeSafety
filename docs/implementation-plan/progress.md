# IvTem.TypeSafety progress log

## Current status

- Phase: Task 2 completed.
- Implementation status: Repository scaffolding created; embedded attribute generator implemented and tested.
- Stop condition: Wait for explicit instruction before Task 3.

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

## Unresolved issues

- Whether to include `IVTS005` for malformed lookalike metadata.
- Whether nested generic type propagation should cross containing type parameters in v1.
- Whether broad semantic use-site coverage should be implemented in one task set or staged after core use sites.
- NuGet analyzer transitivity must be proven by integration tests before documentation claims it.
- Source Link and package contents have not been validated beyond build/restore; Task 13 must inspect the produced `.nupkg`.

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
