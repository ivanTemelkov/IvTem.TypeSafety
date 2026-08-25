# Limitations

## Transitive enforcement boundary

`IvTem.TypeSafety` enforces restrictions only in projects where the analyzer is active.

The tested and supported NuGet/MSBuild cases are:

- direct consumer `PackageReference` to `IvTem.TypeSafety`;
- project-reference consumers when the referenced project has a normal `PackageReference` to `IvTem.TypeSafety`;
- package-reference consumers when the referenced package has a normal dependency on `IvTem.TypeSafety`.

These cases are backed by temporary-project integration tests under `tests/IvTem.TypeSafety.Tests/Packaging/`.

Unsupported or unclaimed cases:

- an intermediate project uses `PrivateAssets="all"` or otherwise prevents `IvTem.TypeSafety` from flowing to consumers;
- a package excludes `buildTransitive` or analyzer assets from dependency flow;
- non-SDK-style or customized builds that do not import NuGet `buildTransitive` assets;
- consumers using a compiler older than the Roslyn API version referenced by the analyzer package.

The package currently references Roslyn `5.6.0` so it can load in the .NET SDK compiler used by the integration tests.
