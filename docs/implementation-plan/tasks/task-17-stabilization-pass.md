# Task 17: Stabilization pass

## Objective

Perform the final v0.1.0 quality pass across diagnostics, docs, tests, package content, performance, and deferred behavior.

## Dependencies

- Tasks 01 through 16 complete.

## Expected affected files

- Any file touched by earlier tasks if stabilization finds a defect.
- `CHANGELOG.md`.
- `AnalyzerReleases.Shipped.md`.
- `AnalyzerReleases.Unshipped.md`.
- `docs/implementation-plan/progress.md`.
- `docs/implementation-plan/decisions.md`.

## Implementation approach

1. Run the full validation suite.
2. Inspect diagnostic IDs, titles, messages, and release tracking files.
3. Inspect README and docs for consistency with implemented behavior.
4. Inspect `.nupkg` and `.snupkg` contents.
5. Review package metadata.
6. Review deferred features and ensure unsupported behavior is documented or tested silent.
7. Review performance-sensitive code paths and synthetic performance tests.
8. Update changelog and analyzer release files for `0.1.0`.
9. Update progress and decisions documentation.
10. Report remaining risks and recommended release step.

## Tests and checks

- `dotnet restore`.
- `dotnet build -c Release`.
- `dotnet test -c Release`.
- `dotnet pack -c Release`.
- Package content validation.
- Sample validation.
- Manual docs/package metadata inspection.

## Acceptance criteria

- Full validation passes.
- Public diagnostic catalog is stable and documented.
- Package is a release candidate for `0.1.0`.
- Remaining limitations are explicit.
- Progress log records final validation and lessons learned.

## Risks and open questions

- Late diagnostic catalog changes may require user review before release.
- Package transitivity limitations may require prominent documentation.
- Performance concerns discovered late may require deferring a feature rather than shipping risky behavior.
