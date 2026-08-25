# C# Coding Style Guide

A practical guide for developers joining the codebase. It documents the conventions that
are **actually used** in the code today, so that new code reads like the code
that is already there.

Target platform: **.NET 10 / C# (latest language version)**. Nullable reference
types and (in the app) implicit usings are enabled.

In the projects use `TreatWarningsAsErrors`.

> There is no `.editorconfig` in the repository. These conventions are enforced
> by convention. If you add one, mirror the rules described here;
> don't change the established style.

---

## 1. File & project layout

- **One public type per file.** The file name matches the type name
  (`OpcUaClient.cs` → `class OpcUaClient`). Small tightly-coupled helpers
  (e.g. a private `EventArgs`) may share a file, but prefer separate files.
- **File-scoped namespaces**, always:

  ```csharp
  namespace IvTem.Common.Attributes;
  ```

- **Namespace mirrors the folder path** exactly. A type in
  `lib/IvTem.Common/Definition/Action/` lives in
  `namespace IvTem.Common.Definition.Action;`.
- **`using` directives go at the top of the file**, above the namespace.
  Some library projects centralise common imports in a `GlobalUsings.cs`
  (`global using ...;`); the app project relies on `<ImplicitUsings>enable</ImplicitUsings>`.
- **No file headers / copyright banners.**
- **Folder vocabulary** is consistent across projects — reuse these names:
  `Feature/` (business logic), `Definition/`, `Model/`, `Extensions/`,
  `Interfaces/`, `Exceptions/`, `Configuration/`, `Repository/`, `Controllers/`,
  `Attributes/`.

## 2. Formatting

- **4-space indentation**, no tabs.
- **Allman braces** — opening brace on its own line.
- A single-statement `if`/`else`/loop body is written **without braces**, on the
  next line:

  ```csharp
  if (firstRender == false)
      return;
  ```

- Fluent / LINQ chains break **before** the `.`, one call per line, indented:

  ```csharp
  var index = configuration.LoggingRules
      .Select((rule, i) => (i, rule))
      .Where(x => x.rule.LoggerNamePattern.Equals("*", StringComparison.Ordinal))
      .Select(x => x.i)
      .FirstOrDefault();
  ```

- **No `this.` qualifier.**
- Negate booleans with **`== false`**, not the `!` operator — this is a
  deliberate, pervasive convention because `!` is easy to miss:

  ```csharp
  if (CheckConnection(out var session) == false)
      return Result.Failed<OpcUaValue>("Client is NOT connected!");
  ```

## 3. Naming

| Element | Convention | Example |
|---|---|---|
| Class / struct / record / method / property | `PascalCase` | `OpcUaClientManager`, `ReadTag` |
| Interface | `I` + `PascalCase` | `IOpcUaClient`, `INavigationHierarchyProvider` |
| Constant / `static readonly` | `PascalCase` | `StorageKey`, `DefaultName` |
| Parameter / local variable | `camelCase` | `connectionString`, `nodeId` |
| **Private backing field** | **`camelCase`, no underscore** | `private string? selectedClientName;` |
| Generic type parameter | `T`, or `T`-prefixed descriptive | `T`, `TPresentedType` |
| Async method | **No `Async` suffix** (see note) | `ReadTag`, `Connect` |

- **Private fields do _not_ use an `_` prefix.** Backing fields are plain
  `camelCase`. This differs from the common .NET `_field` convention — follow
  the local style.
- **Prefer a private auto-property over a field** for dependencies and state,
  even private ones (note the `PascalCase`):

  ```csharp
  private ILogger<OpcUaClient> Logger { get; }
  private Session? Session { get; set; }
  private ConcurrentDictionary<OpcUaNodeId, MonitoredOpcUaTag> MonitoredTagsByNodeId { get; } = new();
  ```

  Use an explicit `camelCase` backing field only when a property needs custom
  get/set logic.
- **Do not append an `Async` suffix to async methods.** This is the local
  convention — async methods are named for what they do (`ReadTag`, `Connect`,
  `LoadFilterFromStorage`), not for being async. Only add the suffix in the rare
  case where a synchronous and an asynchronous overload of the *same* operation
  coexist and must be told apart (e.g. `Save` / `SaveAsync`); even that is
  uncommon. (External frameworks may impose their own names, e.g. `RunAsync`.)

## 4. Types & language features

- **`sealed` by default.** Only leave a class open when it is explicitly meant
  to be a base class.
- **`record` for data** — DTOs, options, value objects, event payloads — using
  positional/primary-constructor syntax:

  ```csharp
  public record OpcUaValue(object? Data, TagState State, DateTime TimeStamp);

  public record HomePageOptions : OptionsBase
  {
      public string ConnectionString { get; init; } = string.Empty;
      public string? ApplicationInstance { get; init; }
  }
  ```

  Use `with` expressions to derive modified copies:
  `opcUaValue with { State = TagState.Failed }`.
- **`required` + `init`** for mandatory immutable properties:
  `public required OpcUaNodeId NodeId { get; init; }`.
- **Expression-bodied members** for one-liners (constructors, properties,
  short methods):

  ```csharp
  public DateTime UtcNow => DateTime.UtcNow;
  public static NavigationHierarchy CreateRoot()
      => new(Root, parentName: null, LearnIcons.Default);
  ```

- **`var` when the type is obvious** from the right-hand side; an explicit type
  otherwise.
- **Collection expressions**: `[]` for empty, `[.. source]` to materialise:

  ```csharp
  public ImmutableArray<string> GetClientNames() => [.. ClientsByName.Keys];
  private ImmutableArray<string> AvailableClients { get; set; } = [];
  ```

- **Switch expressions / pattern matching** over if-else ladders.
- **`nameof(...)`** instead of string literals for member/section names.
- **`Guid.CreateVersion7()`** for new identifiers (not `Guid.NewGuid()`).
- **Always pass a `StringComparison` / `StringComparer`** to string and
  dictionary operations (`StringComparison.Ordinal`,
  `StringComparer.OrdinalIgnoreCase`). Sonar enforces this.

## 5. Immutability & collections

- **Prefer immutable types and collections** on public/return surfaces. Internal
  mutable state (`ConcurrentDictionary`, `List`) stays private.
- **No public setters on collections** — initialise once, return immutable
  snapshots.
- Prefer `init`-only properties for objects that should not change after
  construction. The only exception is the serializable objects.

## 6. Nullability

- Nullable reference types are **enabled solution-wide** (`<Nullable>enable</Nullable>`).
  Keep the code null-warning-free.
- Annotate intent with `System.Diagnostics.CodeAnalysis` attributes:
  `[NotNull]` on parameters, `[NotNullWhen(true)]` on `out` params,
  `[MaybeNull]`, etc.

  ```csharp
  public ImmutableArray<OpcUaTag> AddMonitoredTags([NotNull] IEnumerable<MonitoredTagInfo> tagInfos)
  bool TryGetNode(string key, [NotNullWhen(true)] out DependencyTreeNode<T>? node);
  ```

- **Guard clauses** with the framework throw-helpers at the top of the method:
  `ArgumentNullException.ThrowIfNull(x);`,
  `ArgumentException.ThrowIfNullOrEmpty(name);`.
- Use the null-forgiving `!` only where genuinely justified (notably the Blazor
  `[Inject]` / `[Parameter]` initialisers — see §10).

## 7. Async & concurrency

- All asynchronous methods return **`Task` / `Task<T>`** (or `ValueTask` for hot
  paths). **Do not add an `Async` suffix** — name async methods for what they do;
  add the suffix only to disambiguate a sync/async pair of the same operation,
  which is rare (see §3). **Never `async void`** (except event handlers that have
  no alternative).
- **Be explicit about `.ConfigureAwait(continueOnCapturedContext: true|false)` when awaiting calls.**:

  ```csharp
  var result = await session.SimpleReadAsync(nodeId, Attributes.Value)
      .ConfigureAwait(continueOnCapturedContext: false);
  await app.RunAsync()
      .ConfigureAwait(continueOnCapturedContext: false);
  ```

- **Flow `CancellationToken`** through async APIs.
- Guard shared state with `lock` over a `private static readonly Lock` (the
  .NET 9+ `System.Threading.Lock` type), and dispose
  `CancellationTokenSource`es you create.

## 8. Custom Exceptions

- **Custom exceptions are `sealed`** and live in an `Exceptions/` folder; many
  expose a static `Throw(...)` helper:

  ```csharp
  public sealed class UnexpectedNullException : Exception
  {
      public UnexpectedNullException(string argumentName, Exception? e = null)
          : base($"{argumentName} is null!", e) { }
      public static void Throw(string argumentName, Exception? e = null)
          => throw new UnexpectedNullException(argumentName, e);
  }
  ```

## Quick checklist before you commit

- [ ] File-scoped namespace; namespace matches folder; one type per file.
- [ ] `sealed` unless designed for inheritance; `record` for data.
- [ ] Private fields `camelCase` (no `_`); prefer private auto-properties.
- [ ] `== false` for negation; no `this.`.
- [ ] `var` only when the type is obvious.
- [ ] async methods are **not** `Async`-suffixed.
- [ ] Strings/dictionaries pass an explicit `StringComparison`/`StringComparer`.
