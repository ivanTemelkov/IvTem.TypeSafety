# IvTem.TypeSafety

`IvTem.TypeSafety` is a planned Roslyn analyzer and source-generator package for expressing compile-time restrictions on generic type arguments that C# generic constraints cannot represent directly.

This project is AI-assisted. Planning and implementation are assisted by OpenAI Codex, with human review and ownership by Ivan Temelkov.

## Status

The repository is currently scaffolded only. Analyzer and source-generator behavior will be implemented in later tasks.

## Package Goals

- Generate internal attributes in consuming compilations under `IvTem.TypeSafety`.
- Report compile-time diagnostics when restricted generic type arguments are used.
- Ship as analyzer/source-generator assets without a runtime library dependency.

## Development

Restore and build the repository with:

```powershell
dotnet restore
dotnet build
```
