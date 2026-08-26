# IvTem.TypeSafety

`IvTem.TypeSafety` is a Roslyn analyzer and source-generator package for expressing compile-time restrictions on generic type arguments that C# generic constraints cannot represent directly.

This project is AI-assisted. Planning and implementation are assisted by OpenAI Codex, with human review and ownership by Ivan Temelkov.

## Status

The repository contains the `0.1.2` analyzer/source-generator package, package-content validation, NuGet/MSBuild transitive-enforcement integration tests, and a runnable sample.

## Installation

Install the package in projects that declare or consume restricted generic APIs:

```xml
<PackageReference Include="IvTem.TypeSafety" Version="0.1.2" />
```

The package generates internal attributes into each consuming compilation:

- `IvTem.TypeSafety.DisallowTypesAttribute`
- `IvTem.TypeSafety.DisallowExactTypesAttribute`

No runtime assembly reference is required.

## Quick Examples

Use `DisallowTypes` when derived, implemented, boxed, or otherwise definitely assignable type arguments should be rejected:

```csharp
using IvTem.TypeSafety;

public sealed class Result<[DisallowTypes(typeof(System.Exception))] T>
{
}

Result<string> allowed = new();
// Result<System.InvalidOperationException> rejected = new(); // IVTS001
```

Use `DisallowExactTypes` when only the exact semantic type should be rejected:

```csharp
using IvTem.TypeSafety;

public sealed class Payload<[DisallowExactTypes(typeof(string))] T>
{
}

Payload<object> allowed = new();
// Payload<string> rejected = new(); // IVTS001
```

Restrictions can be declared on generic types and generic methods, and supported contracts propagate through generic inheritance, interface implementation, method overrides, interface methods, and direct declaration signatures such as `Wrapper<T>` containing a `Result<T>` member.

See `samples/IvTem.TypeSafety.Sample/` for a runnable project with valid usage and intentionally invalid examples kept out of compilation.

## Diagnostics

All diagnostics are reported as errors:

| ID | Meaning |
| --- | --- |
| `IVTS001` | A constructed generic type or method uses a forbidden type argument. |
| `IVTS002` | A restriction attribute is configured with an invalid type list or unsupported forbidden type. |
| `IVTS003` | Generic signature propagation contains an unsupported cycle. |
| `IVTS004` | Reserved descriptor for contradictory restriction diagnostics; no current analyzer path emits it in `0.1.2`. |
| `IVTS005` | A current-source lookalike attribute uses the `IvTem.TypeSafety` metadata name but not the expected v1 shape. |

See `docs/diagnostics.md` for examples and remediation guidance.

## Package Shape

`IvTem.TypeSafety` is packaged as a Roslyn analyzer/source-generator package. The `.nupkg` places `IvTem.TypeSafety.dll` under `analyzers/dotnet/cs/netstandard2.0/` and intentionally does not include a runtime `lib/` assembly.

The package also includes `README.md`, MIT license metadata, Git repository metadata, and a `.snupkg` symbol package. Version `0.1.1` aligned the analyzer DLL and PDB under the same package path so nuget.org can validate the symbols package. Version `0.1.2` lowers the Roslyn dependency baseline to support older .NET 8 development environments.

The package includes a `buildTransitive` props asset so downstream projects can receive the analyzer when `IvTem.TypeSafety` flows as a normal transitive package dependency. See `docs/limitations.md` for the precise tested boundary.

## Limitations

Version `0.1.2` intentionally uses definite-only analysis. It does not analyze reflection-created generic types, XML documentation `cref` values, type alias declarations, method-body contract propagation, transformed generic mappings such as `Data<List<T>>`, or generic constraint chains such as `where T : U where U : Exception`.

See `docs/architecture.md` for the implementation model and `docs/limitations.md` for deferred scenarios.

## Development

Restore and build the repository with:

```powershell
dotnet restore IvTem.TypeSafety.slnx
dotnet build IvTem.TypeSafety.slnx --no-restore
dotnet test IvTem.TypeSafety.slnx --no-build
dotnet pack -c Release
```
