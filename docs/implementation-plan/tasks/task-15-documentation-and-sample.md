# Task 15: Documentation and sample

## Objective

Complete user-facing documentation and a runnable sample that demonstrates valid package usage and documents invalid examples.

## Dependencies

- Behavior and diagnostics stabilized through Task 14.

## Expected affected files

- `README.md`.
- `docs/architecture.md`.
- `docs/diagnostics.md`.
- `docs/limitations.md`.
- `CHANGELOG.md`.
- `samples/IvTem.TypeSafety.Sample/`.
- `docs/implementation-plan/progress.md`.

## Implementation approach

1. Expand README:
   - visible AI-assisted/OpenAI Codex statement;
   - purpose;
   - installation;
   - quick examples;
   - diagnostics summary;
   - limitations;
   - transitive packaging behavior.
2. Write architecture documentation:
   - generator/analyzer split;
   - policy model;
   - matching;
   - propagation;
   - cycle handling;
   - cross-assembly metadata.
3. Write diagnostic documentation for each stable `IVTS` ID.
4. Write limitations and deferred feature documentation.
5. Create runnable sample project with valid code.
6. Include intentionally invalid examples as comments or excluded files so the sample build remains green.
7. Validate docs and sample.
8. Update progress documentation.

## Tests and checks

- `dotnet build samples/IvTem.TypeSafety.Sample`.
- `dotnet test` for docs-sensitive diagnostic examples where practical.
- Manual link/path check for repository docs.
- Package README inclusion rechecked by Task 13 validation if metadata changed.

## Acceptance criteria

- Required documentation topics from the specification are covered.
- README visibly states the AI-assisted/OpenAI Codex requirement.
- Sample project builds and demonstrates both attributes.
- Invalid examples are clear without breaking the runnable sample.

## Risks and open questions

- Documentation can drift from implemented diagnostics; keep examples tied to tests where practical.
- README can become too long; move deep material to docs and link it.
- Intentionally invalid examples must not be accidentally compiled in CI.
