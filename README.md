# IvTem.TypeSafety

`IvTem.TypeSafety` is a planned Roslyn analyzer and source-generator package for expressing compile-time restrictions on generic type arguments that C# generic constraints cannot represent directly.

This project is AI-assisted. Planning and implementation are assisted by OpenAI Codex, with human review and ownership by Ivan Temelkov.

## Status

The repository currently contains the initial analyzer/source-generator implementation, package-content validation, and NuGet/MSBuild transitive-enforcement integration tests for the `0.1.0` package.

## Package Goals

- Generate internal attributes in consuming compilations under `IvTem.TypeSafety`.
- Report compile-time diagnostics when restricted generic type arguments are used.
- Ship as analyzer/source-generator assets without a runtime library dependency.

## Package Shape

`IvTem.TypeSafety` is packaged as a Roslyn analyzer/source-generator package. The `.nupkg` places `IvTem.TypeSafety.dll` under `analyzers/dotnet/cs/` and intentionally does not include a runtime `lib/` assembly.

The package also includes `README.md`, MIT license metadata, Git repository metadata, and a `.snupkg` symbol package.

The package includes a `buildTransitive` props asset so downstream projects can receive the analyzer when `IvTem.TypeSafety` flows as a normal transitive package dependency. See `docs/limitations.md` for the precise tested boundary.

## Development

Restore and build the repository with:

```powershell
dotnet restore
dotnet build
dotnet pack -c Release
```
