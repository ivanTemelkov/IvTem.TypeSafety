# Task 01: Repository scaffolding

## Objective

Create the minimal repository structure required to build, test, pack, and document `IvTem.TypeSafety` without implementing analyzer behavior yet.

## Dependencies

- Explicit approval to begin implementation.
- Approved or consciously deferred answers for planning questions that affect project structure.

## Expected affected files

- `IvTem.TypeSafety.slnx` or equivalent solution file.
- `Directory.Build.props`.
- `Directory.Packages.props`.
- `src/IvTem.TypeSafety/IvTem.TypeSafety.csproj`.
- `tests/IvTem.TypeSafety.Tests/IvTem.TypeSafety.Tests.csproj`.
- `README.md`.
- `CHANGELOG.md`.
- `AnalyzerReleases.Shipped.md`.
- `AnalyzerReleases.Unshipped.md`.
- Initial folders under `src/IvTem.TypeSafety/` and `tests/IvTem.TypeSafety.Tests/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Confirm installed SDKs with `dotnet --info`; do not install anything if required SDKs are missing.
2. Create the solution and project skeleton.
3. Configure shared build settings:
   - nullable enabled;
   - warnings as errors;
   - deterministic build settings;
   - repository metadata from the actual Git remote;
   - package metadata fixed by the specification.
4. Configure the analyzer project to target `netstandard2.0`.
5. Configure the test project with xUnit and Roslyn test dependencies, using central package management.
6. Add placeholder analyzer release files and changelog.
7. Add a placeholder README with the required visible AI-assisted/OpenAI Codex statement.
8. Add empty source folders only where they communicate ownership boundaries.
9. Update progress documentation.

## Tests and checks

- `dotnet restore`.
- `dotnet build`.
- `git status --short`.

## Acceptance criteria

- The solution restores and builds with no warnings.
- No analyzer or generator behavior is implemented.
- Package metadata is present but package content validation is deferred to Task 13.
- README visibly states the project is AI-assisted and assisted by OpenAI Codex.
- Progress documentation records validation results and any SDK/tooling issues.

## Risks and open questions

- Local machine or CI may not have the required .NET 10 SDK.
- `.slnx` support depends on SDK/tooling version; fallback may be `.sln` if required.
- Roslyn package versions must support `netstandard2.0` analyzer projects and the chosen test harness.
