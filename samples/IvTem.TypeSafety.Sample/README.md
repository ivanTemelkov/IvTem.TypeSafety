# IvTem.TypeSafety.Sample

This sample builds as a normal console application while referencing the local analyzer/source-generator project as an analyzer.

It demonstrates:

- `DisallowTypes` on a generic class and method;
- `DisallowExactTypes` on a generic class, delegate, and local function;
- interface method contract propagation from `IRepository.Save<T>` to `CustomerRepository.Save<T>`;
- valid uses that keep the build green.

`InvalidExamples.cs.txt` contains intentionally invalid examples and is not compiled. Rename it to `.cs` locally only when you want to inspect the expected diagnostics.

Build from the repository root:

```powershell
dotnet build samples/IvTem.TypeSafety.Sample
```
