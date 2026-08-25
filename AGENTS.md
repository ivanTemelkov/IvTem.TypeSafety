# Repository Guidelines

## Project Structure & Module Organization

`IvTem.TypeSafety` is a Roslyn analyzer/source-generator package scaffold. The solution file is `IvTem.TypeSafety.slnx`. Production code belongs under `src/IvTem.TypeSafety/`, with ownership folders such as `Analysis/`, `Attributes/`, `Diagnostics/`, `Generation/`, `Packaging/`, `Policies/`, and `Propagation/`. Tests belong under `tests/IvTem.TypeSafety.Tests/`, grouped by matching feature folders plus `TestInfrastructure/`.

Planning and execution records live in `docs/implementation-plan/`. Execute one task file at a time from `docs/implementation-plan/tasks/`, then update `docs/implementation-plan/progress.md`.

## Build, Test, and Development Commands

- `dotnet restore IvTem.TypeSafety.slnx` restores all projects and central package versions.
- `dotnet build IvTem.TypeSafety.slnx --no-restore` builds the scaffold and treats warnings as errors.
- `dotnet test IvTem.TypeSafety.slnx --no-build` runs the xUnit test project once tests exist.
- `git status --short` verifies the working tree before and after changes.

The analyzer project targets `netstandard2.0`; the test project targets `net10.0`.

## Coding Style & Naming Conventions

Follow `CODING-STYLE.md` as authoritative. Use 4 spaces, no tabs, Allman braces, file-scoped namespaces, and one public type per file. Namespaces must mirror folder paths. Prefer `sealed` types, nullable warning-free code, explicit `StringComparison`/`StringComparer`, and `ConfigureAwait(continueOnCapturedContext: false)` in library awaits. Private fields use `camelCase` without underscores; prefer private auto-properties for dependencies. Boolean negation uses `== false`.

## Testing Guidelines

Use xUnit for tests. Analyzer and generator tests should live near the behavior they prove, for example `Generation/` for emitted attributes and `Analysis/` for diagnostics. Add focused tests with each behavior change, assert diagnostic IDs and useful locations, and keep package-content validation for the packaging task.

## Commit & Pull Request Guidelines

Existing history uses short prefixes such as `new:` and `chore:`. Keep commits concise and task-scoped, for example `new: generator attributes` or `chore: update progress log`.

Pull requests should describe the executed task, list changed areas, include restore/build/test results, call out deviations from the implementation plan, and note unresolved risks. Do not claim analyzer behavior, packaging shape, or transitivity until validated.
