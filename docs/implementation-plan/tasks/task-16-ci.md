# Task 16: CI

## Objective

Add GitHub Actions-compatible CI for build, test, package, package validation, and sample validation.

## Dependencies

- Tasks 13 through 15 complete.

## Expected affected files

- `.github/workflows/ci.yml`.
- Possibly `global.json` if SDK pinning is approved.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Choose SDK setup strategy for .NET 10 primary and .NET 8 secondary compatibility.
2. Add workflow triggers for pull requests and pushes to `main`.
3. Restore dependencies.
4. Build Release with warnings as errors.
5. Run analyzer/generator tests.
6. Run integration/package validation tests.
7. Pack Release.
8. Validate package content.
9. Build sample.
10. Do not publish to NuGet.
11. Update progress documentation.

## Tests and checks

- Run local workflow-equivalent commands:
   - `dotnet restore`;
   - `dotnet build -c Release`;
   - `dotnet test -c Release`;
   - `dotnet pack -c Release`;
   - sample build.
- Inspect workflow YAML for obvious syntax errors.

## Acceptance criteria

- CI covers .NET 10 primary validation.
- CI includes .NET 8 compatibility checks where feasible.
- CI validates package content and sample build.
- CI does not publish packages.

## Risks and open questions

- GitHub hosted runner support for .NET 10 depends on release timing and setup action behavior.
- Full package integration tests may be slow; consider separating unit and integration jobs if needed.
- SDK pinning may help determinism but can create maintenance work.
