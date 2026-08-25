# Task 13: Packaging and package-content validation

## Objective

Produce a NuGet package that ships analyzer/source-generator assets only, with no runtime `lib/` assembly.

## Dependencies

- Tasks 01 through 12 complete enough for packaging.

## Expected affected files

- `src/IvTem.TypeSafety/IvTem.TypeSafety.csproj`.
- `Directory.Build.props`.
- Package validation tests under `tests/IvTem.TypeSafety.Tests/Packaging/`.
- `README.md`.
- `CHANGELOG.md`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Configure package metadata:
   - `PackageId` `IvTem.TypeSafety`;
   - `Version` `0.1.0`;
   - `Authors` `Ivan Temelkov`;
   - MIT license expression or license file;
   - repository URL from Git remote;
   - README file.
2. Configure analyzer output under `analyzers/dotnet/cs/`.
3. Suppress normal runtime `lib/` asset inclusion.
4. Configure symbol package output.
5. Configure deterministic build and Source Link.
6. Pack in Release mode.
7. Add package-content validation that opens the `.nupkg` and asserts required/forbidden entries.
8. Update progress documentation.

## Tests and checks

- `dotnet pack -c Release`.
- Package validation test:
   - no `lib/` entries;
   - analyzer assembly exists under `analyzers/dotnet/cs/`;
   - README included;
   - license metadata present;
   - symbols/source metadata present where expected.
- `dotnet test --filter` for packaging tests.
- `dotnet build -c Release`.

## Acceptance criteria

- The package contains no runtime assembly.
- Analyzer/source-generator assets are included in the expected NuGet analyzer path.
- Package metadata matches the specification.
- Package content validation is automated.

## Risks and open questions

- MSBuild defaults may try to include `lib/` output unless explicitly configured.
- Source Link and symbol package behavior may require package references or CI context.
- Repository URL must not be invented if Git remote changes or disappears.
