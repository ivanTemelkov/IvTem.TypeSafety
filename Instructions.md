# IvTem.TypeSafety — Implementation Planning and Development Prompt

You are working in a new GitHub repository for a NuGet package named **`IvTem.TypeSafety`**.

Your first responsibility is **planning, not implementation**.

---

# 1. Mandatory first steps

Before proposing architecture or modifying implementation files:

1. Read the repository root file:

   `CODING-STYLE.md`

2. Read **every document referenced by `CODING-STYLE.md`**, including the detailed convention files under:

   `docs/agent/`

3. Treat those conventions as authoritative.

4. Inspect the repository and existing Git metadata.

5. Identify any ambiguity or conflict between this specification and the repository conventions.

If a conflict exists, raise it during planning rather than silently choosing one interpretation.

---

# 2. Required workflow

The project must be developed using a plan-first, task-oriented workflow.

## Phase 1 — Planning

Create a detailed implementation plan divided into **small, independently verifiable tasks**.

Each task should specify:

- objective;
- files/components expected to be affected;
- implementation approach;
- tests required;
- acceptance criteria;
- dependencies on previous tasks;
- significant risks or open questions.

Also create persistent repository documentation that records:

- the implementation plan;
- architectural decisions;
- task status;
- validation performed;
- lessons learned;
- unresolved issues;
- deferred features.

These planning/progress documents are intended to remain committed as permanent project documentation.

### Critical rule

After producing the detailed plan:

**STOP.**

Do not implement the package yet.

Wait for explicit approval before beginning implementation.

Creating/updating planning documentation is allowed during this phase, but do not create the actual analyzer/generator implementation before approval.

---

# 3. Execution after plan approval

After the plan is approved:

1. Execute **one task at a time**.
2. Do not automatically continue to the next task.
3. Complete and validate the current task.
4. Update the persistent progress documentation with:
   - work completed;
   - validation results;
   - decisions made;
   - deviations from the original plan;
   - lessons learned;
   - newly discovered risks.
5. Report completion of that task.
6. Wait for instruction before starting the next task.

The implementation plan should be treated as a living document and updated when discoveries require changes.

---

# 4. Product objective

Create a C# compile-time type-safety NuGet package named:

`IvTem.TypeSafety`

The package provides additional restrictions for generic type parameters that cannot currently be expressed with standard C# generic constraints.

The initial use case is preventing `Exception` or types derived from `Exception` from being supplied as a generic payload.

Example:

```csharp
public sealed class Data<
    [DisallowTypes(typeof(Exception))]
    T>
    where T : notnull
{
    public required T Payload { get; init; }
}
```

These should be rejected at compile time:

```csharp
Data<Exception>
Data<InvalidOperationException>
Data<IOException>
```

while these remain valid:

```csharp
Data<string>
Data<int>
Data<MyDto>
```

The system must be generic and reusable beyond `Exception`.

---

# 5. High-level architecture

Use a **hybrid Roslyn architecture** packaged in a single NuGet package:

- an **incremental source generator** that generates the attribute definitions;
- a **DiagnosticAnalyzer** that performs semantic validation and reports compile-time errors.

Do not abuse the source generator as a replacement for an analyzer.

The generator owns source generation.

The analyzer owns semantic enforcement.

Both ship in the same:

`IvTem.TypeSafety`

NuGet package.

Use stable Roslyn APIs only for v1.

---

# 6. Zero runtime dependency requirement

The package must have **no runtime assembly**.

The `.nupkg` should not contain a normal `lib/` runtime assembly.

The Roslyn components should be packaged as analyzer/source-generator assets.

The attributes should be generated directly into consuming compilations.

Package validation should explicitly verify that no runtime library is shipped.

---

# 7. Generated namespace and attributes

Generate attributes under:

```csharp
namespace IvTem.TypeSafety;
```

Do not generate a global using.

Consumers should explicitly use:

```csharp
using IvTem.TypeSafety;
```

or fully qualify the attributes.

Generate two attributes.

## 7.1 `DisallowTypesAttribute`

Conceptually:

```csharp
[DisallowTypes(typeof(Exception))]
T
```

means:

> This generic parameter must not receive the listed type or a type assignable to the listed type according to the v1 matching rules.

## 7.2 `DisallowExactTypesAttribute`

Conceptually:

```csharp
[DisallowExactTypes(typeof(Exception))]
T
```

means:

> This generic parameter must not receive exactly one of the listed types.

Derived or implementing types are not rejected by this attribute.

Example:

```csharp
[DisallowExactTypes(typeof(Exception))]
T
```

results in:

```csharp
Exception                 // rejected
InvalidOperationException // allowed
IOException               // allowed
```

---

# 8. Attribute implementation model

Both generated attributes should:

- target `AttributeTargets.GenericParameter`;
- support `AllowMultiple = true`;
- accept `params Type[]`;
- be generated as internal embedded attributes;
- use the Roslyn `EmbeddedAttribute` mechanism;
- include XML documentation comments suitable for IntelliSense.

Use the appropriate Roslyn embedded-attribute mechanism, including `AddEmbeddedAttributeDefinition()` where appropriate.

The generated names are owned by this package:

- `IvTem.TypeSafety.DisallowTypesAttribute`
- `IvTem.TypeSafety.DisallowExactTypesAttribute`

If a consumer manually defines conflicting types using these names, do not add complicated coexistence logic in v1. Allow normal compiler conflicts to surface.

---

# 9. Multiple attributes and multiple types

Both forms must be supported:

```csharp
[DisallowTypes(
    typeof(Exception),
    typeof(Stream))]
T
```

and:

```csharp
[DisallowTypes(typeof(Exception))]
[DisallowTypes(typeof(Stream))]
T
```

They are semantically equivalent.

The two attribute kinds may also coexist:

```csharp
public sealed class Data<
    [DisallowTypes(typeof(Exception))]
    [DisallowExactTypes(typeof(string), typeof(int))]
    T>
{
}
```

All applicable policies should be accumulated.

Duplicate configured types are allowed and should be de-duplicated semantically rather than reported as configuration errors.

---

# 10. Supported generic declarations

The attributes apply directly to generic parameters.

Support generic parameters belonging to:

- classes;
- structs;
- interfaces;
- delegates;
- methods;
- local functions.

Records and other declaration forms not explicitly specified here must be discussed during planning before assuming semantics.

Example generic method:

```csharp
public class Utility
{
    public T DoStuff<
        [DisallowTypes(typeof(Exception))]
        T>()
    {
        // ...
    }
}
```

---

# 11. `DisallowTypes` matching semantics

The default assignable rule must reject:

- the same type;
- derived classes;
- implementations of forbidden interfaces;
- generic variance relationships;
- array covariance;
- applicable boxing relationships for value types;
- direct generic parameter constraints that definitively prove assignability.

Examples:

```csharp
[DisallowTypes(typeof(Exception))]
T
```

rejects:

```csharp
Exception
InvalidOperationException
IOException
```

---

## 11.1 Interfaces

Example:

```csharp
[DisallowTypes(typeof(IDisposable))]
T
```

must reject:

```csharp
FileStream
```

because `FileStream` implements `IDisposable`.

---

## 11.2 Generic variance

Example:

```csharp
[DisallowTypes(typeof(IEnumerable<object>))]
T
```

must reject:

```csharp
IEnumerable<string>
```

because `IEnumerable<out T>` is covariant.

---

## 11.3 Array covariance

Example:

```csharp
[DisallowTypes(typeof(object[]))]
T
```

must reject:

```csharp
string[]
```

---

## 11.4 Value types and boxing

Example:

```csharp
[DisallowTypes(typeof(ValueType))]
T
```

should reject:

```csharp
int
DateTime
```

Likewise:

```csharp
[DisallowTypes(typeof(IComparable))]
T
```

should reject `int` when the applicable boxing/interface relationship establishes compatibility.

---

## 11.5 Ref-like types

Ref-like / `ref struct` types that implement a forbidden interface should still be considered violations.

Do not make physical boxing ability the sole semantic test.

The analyzer should remain aligned with C# type relationships.

---

# 12. Conversions that must NOT count

`DisallowTypes` must not interpret general C# convertibility as assignability.

Do not reject a type merely because it has:

- user-defined implicit conversions;
- user-defined explicit conversions;
- ordinary numeric conversions.

Example:

```csharp
public sealed class Money
{
    public static implicit operator decimal(Money value) => ...;
}
```

With:

```csharp
[DisallowTypes(typeof(decimal))]
T
```

`Money` must remain allowed.

---

# 13. Special rule for `System.Object`

This configuration is invalid:

```csharp
[DisallowTypes(typeof(object))]
T
```

Report a compile-time **configuration error**.

This special prohibition applies only to:

`System.Object`

Do not generalize it to:

- `System.ValueType`;
- `System.Enum`;
- `System.Delegate`;
- interfaces;
- other broad types.

This remains valid:

```csharp
[DisallowExactTypes(typeof(object))]
T
```

and rejects exactly `object`.

---

# 14. Exact matching semantics

`DisallowExactTypes` uses exact type identity rather than inheritance or interface assignability.

Example:

```csharp
[DisallowExactTypes(typeof(Exception))]
T
```

results in:

```csharp
Exception                 // error
InvalidOperationException // allowed
```

A generic constraint such as:

```csharp
where T : Exception
```

does **not** prove that `T` is exactly `Exception`.

Therefore:

```csharp
public void Foo<T>()
    where T : Exception
{
    DataWithExactRestriction<T> value;
}
```

must remain valid unless the actual type supplied later is definitely the exact forbidden type.

---

# 15. Nullable reference annotations

Nullable reference annotations do not create a distinct runtime type for these checks.

Example:

```csharp
[DisallowExactTypes(typeof(string))]
T
```

must reject both:

```csharp
string
string?
```

Do not allow nullable reference annotations to bypass the restriction.

---

# 16. `dynamic`

Treat `dynamic` as `System.Object` for these checks.

Example:

```csharp
public sealed class Box<
    [DisallowExactTypes(typeof(object))]
    T>
{
}
```

must reject:

```csharp
Box<dynamic>
```

---

# 17. Nullable value types

Nullable value types remain distinct constructed types.

Example:

```csharp
public sealed class Box<
    [DisallowTypes(typeof(int))]
    T>
{
}
```

results in:

```csharp
Box<int>   // error
Box<int?>  // allowed
```

Do not recursively inspect the underlying type merely because it is `Nullable<T>`.

---

# 18. Direct-type-only rule

Restrictions apply to the actual generic argument supplied to the restricted generic parameter.

Do not recursively search inside arbitrary nested type arguments.

Example:

```csharp
public sealed class Data<
    [DisallowTypes(typeof(Exception))]
    T>
{
}
```

results in:

```csharp
Data<Exception>                 // error
Data<InvalidOperationException> // error

Data<List<Exception>>           // allowed
Data<Task<Exception>>           // allowed
```

The direct supplied type `List<Exception>` is not itself assignable to `Exception`.

---

# 19. Supported forbidden type expressions

v1 supports:

- non-generic concrete types;
- interfaces;
- fully closed constructed generic types.

Example:

```csharp
[DisallowTypes(typeof(IEnumerable<string>))]
T
```

is valid.

---

# 20. Invalid attribute configurations

Invalid configuration must itself produce compile-time errors.

At minimum reject:

```csharp
[DisallowTypes()]
[DisallowExactTypes()]
```

and equivalent configurations containing no usable type entries.

Reject null arrays or null entries where representable.

Reject open/unbound generic types:

```csharp
[DisallowTypes(typeof(IEnumerable<>))]
T
```

Reject forbidden types containing generic parameters from the surrounding declaration:

```csharp
class Container<T,
    [DisallowTypes(typeof(IEnumerable<T>))]
    U>
{
}
```

Reject:

```csharp
[DisallowTypes(typeof(object))]
```

as described earlier.

Do not silently ignore malformed configuration.

---

# 21. Generic type arguments that are themselves type parameters

Follow the **definite violation only** rule.

Example:

```csharp
public void Process<T>()
{
    Data<T> data = new();
}
```

where `Data<TPayload>` has:

```csharp
[DisallowTypes(typeof(Exception))]
```

must be allowed because the analyzer cannot prove that `T` is an exception.

---

# 22. Direct generic constraints

A direct generic constraint may establish a definite assignable violation.

Example:

```csharp
public void Foo<T>()
    where T : Exception
{
    Data<T> value;
}
```

must be rejected if `Data<TPayload>` has:

```csharp
[DisallowTypes(typeof(Exception))]
```

because every legal `T` is known to be assignable to `Exception`.

Direct interface constraints should behave equivalently where applicable.

---

# 23. Generic constraint reasoning intentionally NOT supported in v1

Keep v1 simple.

Do **not** implement special reasoning for:

```csharp
where T : struct
where T : unmanaged
where T : class
where T : notnull
```

even if such a constraint might theoretically prove compatibility with a broad forbidden type.

Also do **not** follow generic-parameter constraint chains transitively.

Example:

```csharp
where T : U
where U : Exception
```

must not receive special transitive reasoning in v1.

Document these as future enhancement candidates.

The architecture should nevertheless avoid making future support unnecessarily difficult.

---

# 24. Contradictory generic declarations

If supported direct constraint reasoning proves that an annotated generic parameter can never have a valid type argument, report a compile-time configuration error on the declaration.

Example:

```csharp
public void Process<
    [DisallowTypes(typeof(Exception))]
    T>()
    where T : Exception
{
}
```

is self-contradictory and should fail at declaration time.

Do not extend this v1 feature using special constraint reasoning that has explicitly been deferred.

---

# 25. Explicit and inferred generic method arguments

Both explicit and inferred generic arguments must be validated.

Example:

```csharp
public static void Process<
    [DisallowTypes(typeof(Exception))]
    T>(T value)
{
}
```

Both should fail:

```csharp
Process<InvalidOperationException>(exception);
Process(exception);
```

when inference determines:

```csharp
T == InvalidOperationException
```

---

# 26. Method groups and delegates

A violation exists when the forbidden closed generic method is formed, even if it is not invoked.

Example:

```csharp
Action action =
    Process<InvalidOperationException>;
```

must be diagnosed.

---

# 27. Semantic usage coverage

A restriction is a contract of the generic parameter.

A violation should be detected at semantic uses of the constructed generic declaration, not merely at object construction.

Examples that should be supported include:

```csharp
Data<Exception> field;

Data<Exception> Property { get; set; }

void M(Data<Exception> value) { }

Data<Exception> M2() => default!;

typeof(Data<Exception>);

new Data<Exception>();
```

Base/interface usages must also be checked:

```csharp
class Derived :
    SomeGenericBase<Exception>
{
}
```

Do not narrowly implement only `ObjectCreationExpressionSyntax`.

Prefer semantic analysis over syntax-name matching.

---

# 28. Diagnostic locations

For explicitly written generic type arguments, report the error as precisely as practical on the offending argument.

Example:

```csharp
Data<InvalidOperationException> value;
     ^^^^^^^^^^^^^^^^^^^^^^^^^
```

For explicit generic methods:

```csharp
Process<InvalidOperationException>();
        ^^^^^^^^^^^^^^^^^^^^^^^^^
```

For inferred generic methods where no type argument syntax exists, report the diagnostic on the method/invocation location and clearly state the inferred offending type in the diagnostic message.

For configuration errors, choose a precise location on the invalid attribute argument or declaration where practical.

---

# 29. One diagnostic per offending generic argument

When one supplied generic argument violates several configured rules, emit **one diagnostic for that generic argument**, not several diagnostics over the same source span.

Example:

```csharp
[DisallowTypes(
    typeof(Exception),
    typeof(ISerializable))]
T
```

and:

```csharp
MySerializableException :
    Exception,
    ISerializable
```

should produce one violation diagnostic.

The message may identify multiple matched forbidden types.

The order must be deterministic.

---

# 30. Diagnostic severity and stability

Violations are:

`DiagnosticSeverity.Error`

Invalid configuration is also an error.

Diagnostics should use the prefix:

`IVTS`

The concrete diagnostic IDs and messages are **not yet finalized**.

During planning:

- propose a coherent diagnostic catalog;
- propose IDs;
- propose messages;
- propose diagnostic locations;
- explain which IDs represent stable user-facing contracts.

Do not finalize implementation until this diagnostic catalog is reviewed.

Once accepted, diagnostic IDs and meanings are stable public API.

Use standard Roslyn suppression/configuration mechanisms.

Do not attempt to make diagnostics artificially unsuppressible.

---

# 31. Attribute rules from referenced assemblies

Restrictions must work when an annotated generic declaration comes from a referenced assembly/NuGet package.

Example:

Assembly A:

```csharp
public sealed class Data<
    [DisallowTypes(typeof(Exception))]
    T>
{
}
```

Assembly B references A:

```csharp
Data<IOException> value;
```

Assembly B should receive the compile-time error when the analyzer is active.

Because generated attributes are embedded separately into each compilation, do not rely on shared CLR type identity.

Recognize the attributes reliably by their stable metadata contract / fully qualified names and validate the expected shape where appropriate.

---

# 32. Overrides and interface implementations

Restrictions are part of the semantic generic contract.

They must propagate through overrides and interface implementations.

Example:

```csharp
public interface IUtility
{
    T DoStuff<
        [DisallowTypes(typeof(Exception))]
        T>();
}
```

and:

```csharp
public sealed class Utility : IUtility
{
    public T DoStuff<T>() => default!;
}
```

must still cause:

```csharp
new Utility()
    .DoStuff<InvalidOperationException>();
```

to fail.

The attribute does not need to be physically copied to the implementing declaration.

The analyzer must reason about the inherited contract.

---

# 33. Generic type inheritance/interface propagation

Restrictions must propagate through generic type inheritance and interface implementation.

Example:

```csharp
public interface IContainer<
    [DisallowTypes(typeof(Exception))]
    T>
{
}

public sealed class Container<T> :
    IContainer<T>
{
}
```

must effectively make:

```csharp
Container<InvalidOperationException>
```

invalid.

---

# 34. Transitive propagation

Propagation must be transitive across inheritance/interface layers.

Example:

```csharp
public interface IBase<
    [DisallowTypes(typeof(Exception))]
    T>
{
}

public interface IMiddle<T> :
    IBase<T>
{
}

public sealed class Concrete<T> :
    IMiddle<T>
{
}
```

must reject:

```csharp
Concrete<InvalidOperationException>
```

---

# 35. Multiple inherited contracts

When restrictions arrive from multiple base types/interfaces/member contracts, combine them as a union.

Example:

```csharp
public interface IA<
    [DisallowTypes(typeof(Exception))]
    T>
{
}

public interface IB<
    [DisallowExactTypes(typeof(string))]
    T>
{
}

public sealed class Combined<T> :
    IA<T>,
    IB<T>
{
}
```

must reject both:

```csharp
Combined<IOException>
Combined<string>
```

---

# 36. Violations in base/interface declarations

If a base/interface declaration already supplies a definitely forbidden concrete type, report the violation immediately.

Example:

```csharp
public interface IContainer<
    [DisallowTypes(typeof(Exception))]
    T>
{
}

public sealed class BrokenContainer<T> :
    IContainer<InvalidOperationException>
{
}
```

must fail at the declaration.

---

# 37. Partial declarations

Restrictions appearing on different partial declarations must be combined.

Example:

File 1:

```csharp
public partial class Container<
    [DisallowTypes(typeof(Exception))]
    T>
{
}
```

File 2:

```csharp
public partial class Container<
    [DisallowExactTypes(typeof(string))]
    T>
{
}
```

must effectively apply both policies.

---

# 38. Signature-based generic composition propagation

v1 must support **direct generic-parameter propagation through declaration signatures**.

Example:

```csharp
public sealed class Data<
    [DisallowTypes(typeof(Exception))]
    T>
{
}

public sealed class Wrapper<T>
{
    public Data<T> Value { get; set; }
}
```

The restriction should propagate from `Data<T>` to `Wrapper<T>`.

Therefore:

```csharp
Wrapper<InvalidOperationException>
```

must fail.

This is necessary to prevent an unannotated generic wrapper from becoming an escape hatch.

---

# 39. Signature positions participating in propagation

Signature-based propagation applies regardless of accessibility or static/instance status.

Inspect applicable declaration signatures including:

- fields;
- properties;
- method parameters;
- method return types;
- events;
- static members;
- instance members;
- private members;
- public members;
- generic constraints/signature types where applicable.

Example:

```csharp
public sealed class Wrapper<T>
{
    private Data<T> _value;

    private static Data<T>? Cache;

    public Data<T> Get() => default!;

    public void Set(Data<T> value) { }

    public event Action<Data<T>>? Changed;
}
```

The `Data<T>` restriction should propagate to `Wrapper<T>`.

When walking nested signature types such as:

```csharp
Action<Data<T>>
```

the important direct mapping is still:

```text
Data restricted parameter -> Wrapper<T>
```

---

# 40. Direct mapping only for v1

Do not perform general symbolic type-expression reasoning.

Support direct mapping such as:

```csharp
Data<T>
```

where the restricted parameter maps directly to the containing generic parameter `T`.

Do not propagate through transformations such as:

```csharp
Data<List<T>>
Data<T[]>
Data<(T, string)>
Data<SomeWrapper<T>>
```

unless another already-defined v1 rule independently requires it.

These more complex symbolic mappings are future-version candidates.

---

# 41. Method-body propagation is NOT supported in v1

This:

```csharp
public sealed class Wrapper<T>
{
    public void Run()
    {
        Data<T> temporary = new();
    }
}
```

must **not** cause `Wrapper<T>` itself to inherit the restriction in v1 merely because `Data<T>` appears in a method body.

The direct `Data<T>` use in that body may still be analyzed according to ordinary use-site rules, but body-local usage must not alter the public/structural contract of `Wrapper<T>`.

Record body-based propagation as a future-version feature.

---

# 42. Cyclic generic-signature graphs

v1 deliberately avoids recursive/fixed-point propagation complexity.

If the analyzer detects **any cyclic generic-signature propagation graph**, report a compile-time error.

Example:

```csharp
public sealed class A<T>
{
    public B<T> Value { get; set; }
}

public sealed class B<T>
{
    public A<T> Value { get; set; }
}
```

In v1, a cyclic generic-signature graph detected by the analyzer is invalid.

Do not risk infinite recursion.

Do not attempt full cycle/fixed-point reasoning in v1.

Document correct cycle management as a future-version enhancement.

During planning, define:

- exactly what constitutes a graph node;
- exactly what constitutes a propagation edge;
- how cycles are detected;
- where the diagnostic is reported;
- how duplicate cycle diagnostics are avoided.

---

# 43. Generated code

Do not analyze generated code produced by other generators in v1.

Configure analyzer behavior accordingly.

Normal user-authored source consuming generated declarations should still be analyzed.

---

# 44. Explicitly out of scope for v1

The following scenarios are intentionally deferred.

Document them clearly as potential future enhancements.

## 44.1 Type aliases

Example:

```csharp
using BadData =
    Data<InvalidOperationException>;
```

Alias-specific enforcement is not required in v1.

## 44.2 Reflection-created generic types

Example:

```csharp
typeof(Data<>)
    .MakeGenericType(
        typeof(InvalidOperationException));
```

Reflection/data-flow analysis is out of scope.

## 44.3 XML documentation `cref`

Example:

```csharp
/// <see cref="Data{Exception}"/>
```

No v1 enforcement is required.

## 44.4 Method-body contract propagation

As described earlier.

## 44.5 Nested symbolic generic propagation

Examples:

```csharp
Data<List<T>>
Data<T[]>
```

## 44.6 Generic constraint chains

Example:

```csharp
where T : U
where U : Exception
```

## 44.7 Special constraint reasoning

Examples:

```csharp
where T : struct
where T : unmanaged
```

## 44.8 Correct cyclic/fixed-point propagation

v1 reports cycles as errors instead.

Keep explicit trace of all these scenarios in future-work documentation.

---

# 45. Analyzer performance

Performance is an explicit acceptance criterion.

Design for incremental and efficient analysis.

Requirements include:

- avoid unnecessary compilation-wide syntax scans;
- prefer semantic/symbol-based analysis where appropriate;
- cache immutable policy information where safe;
- enable concurrent analyzer execution;
- avoid repeated expensive symbol graph traversal;
- prevent recursion problems;
- make results deterministic;
- include performance-oriented tests using synthetic larger compilations.

A dedicated BenchmarkDotNet project is not required in v1.

There is no required code-coverage threshold.

---

# 46. Compatibility

The Roslyn analyzer/generator assembly should target:

`netstandard2.0`

Primary environment:

- .NET 10
- C# 14-era tooling

Secondary compatibility environment:

- .NET 8

Generated source should avoid unnecessarily requiring new language syntax when simpler syntax provides broader compatibility.

Use stable Roslyn APIs only.

C# is the only supported source language in v1.

Do not implement Visual Basic analyzer support.

---

# 47. NuGet/transitive enforcement goal

The restriction is intended to behave as part of a library's public generic contract.

Example:

```text
Application B
    -> Library A
        -> IvTem.TypeSafety
```

If Library A exposes an annotated generic API, the goal is for enforcement to reach downstream consumers where NuGet/MSBuild technically allows it.

Do not assume analyzer assets automatically flow transitively.

During planning:

- investigate the strongest practical NuGet/MSBuild design;
- consider `buildTransitive` or equivalent mechanisms;
- create integration tests proving actual behavior;
- distinguish package references from project references;
- document any scenario where transparent transitive enforcement is technically impossible.

Do not claim transitivity unless it is proven by tests.

---

# 48. Documentation requirements

Documentation is a required implementation phase, not an afterthought.

Create detailed documentation explaining:

- purpose;
- installation;
- architecture;
- generated attributes;
- `DisallowTypes`;
- `DisallowExactTypes`;
- exact vs assignable behavior;
- multiple attributes;
- multiple forbidden types;
- generic classes;
- generic structs;
- generic interfaces;
- delegates;
- generic methods;
- local functions;
- explicit type arguments;
- inferred type arguments;
- inheritance;
- interface implementation;
- cross-assembly behavior;
- signature-based propagation;
- configuration errors;
- diagnostics;
- limitations;
- deferred features;
- transitive packaging behavior;
- performance considerations.

Include practical examples.

---

# 49. README

Create a root:

`README.md`

It should also be embedded as the NuGet package README.

The README must clearly state that the project is an **AI-powered / AI-assisted project**.

It should explicitly state that the project was designed and implemented with assistance from:

**OpenAI Codex**

Do not hide this in an obscure section.

The statement should be clear and visible.

---

# 50. Runnable samples

Create a runnable sample project demonstrating correct usage.

Include valid examples and clear examples of invalid code, using comments/documentation where intentionally uncompilable examples cannot live directly in the runnable build.

The sample should demonstrate both attributes and common scenarios.

---

# 51. Diagnostic documentation

Use the reserved diagnostic prefix:

`IVTS`

During planning, propose the concrete diagnostic catalog.

Once approved:

- document every diagnostic;
- keep diagnostic IDs stable;
- test diagnostic IDs/messages/locations;
- maintain standard Roslyn analyzer release tracking files:

```text
AnalyzerReleases.Shipped.md
AnalyzerReleases.Unshipped.md
```

Also maintain:

`CHANGELOG.md`

---

# 52. Known package metadata

Use:

```text
PackageId: IvTem.TypeSafety
Version: 0.1.0
Authors: Ivan Temelkov
Company: omitted
License: MIT
```

Add an MIT `LICENSE`.

Repository metadata must be derived from the actual Git remote if available.

Do not invent repository URLs.

Other repository, Git, CI, release, and versioning details that have not already been decided should be raised during planning rather than invented.

---

# 53. CI expectations already agreed

The repository is hosted on GitHub.

Create GitHub Actions-compatible CI.

The broad expectations are:

- .NET 10 primary testing;
- .NET 8 secondary testing;
- Release build;
- analyzer/generator tests;
- integration tests;
- package creation;
- package-content validation;
- sample validation;
- no automatic NuGet publication in v1.

Support:

- deterministic builds;
- Source Link;
- symbol package (`.snupkg`);
- GitHub repository metadata where available.

There is no mandatory code-coverage threshold.

If implementation details or additional CI decisions are required, raise them during planning.

---

# 54. No code fix in v1

Do not implement a Roslyn `CodeFixProvider` in v1.

There is generally no universally correct automatic replacement for a forbidden generic type argument.

---

# 55. Testing expectations

The plan should include a thorough analyzer/generator test matrix.

At minimum cover:

### Attribute generation

- both attributes generated;
- namespace correct;
- internal visibility;
- embedded attribute behavior;
- XML documentation;
- multiple attributes;
- multiple arguments.

### Assignable checks

- exact same class;
- derived class;
- interface implementation;
- variance;
- array covariance;
- boxing/value types;
- ref-like interface implementation;
- user-defined conversion does not match;
- numeric conversion does not match.

### Exact checks

- exact match rejected;
- derived class accepted;
- nullable reference annotation does not bypass;
- `dynamic` behaves as `object`;
- nullable value type remains distinct.

### Invalid configuration

- empty type list;
- null input where representable;
- open generic;
- forbidden type containing generic parameters;
- `DisallowTypes(typeof(object))`;
- contradictory declaration.

### Generic methods

- explicit arguments;
- inferred arguments;
- method groups;
- local functions.

### Use sites

- fields;
- properties;
- parameters;
- return types;
- `typeof`;
- object construction;
- base/interface declarations;
- other common semantic type-use locations identified during planning.

### Contract propagation

- override;
- interface implementation;
- generic interface implementation;
- generic base class;
- multiple interfaces;
- transitive inheritance;
- partial declarations;
- direct signature propagation;
- private members;
- static members;
- nested signature containers such as `Action<Data<T>>`;
- transitive direct-mapping propagation.

### Cycle behavior

- simple cycle;
- longer cycle;
- deterministic diagnostic;
- no stack overflow;
- no duplicate storm.

### Cross assembly

- declaration and consumption in different compilations;
- metadata attribute recognition;
- package/project reference scenarios;
- transitive analyzer packaging where feasible.

### Deferred behavior

Include tests or documentation assertions proving that unsupported v1 behavior is intentionally not implemented where useful.

---

# 56. Important implementation principle

Do not base correctness primarily on syntax text such as:

```text
"Data"
"Exception"
```

Use Roslyn semantic symbols.

Important concepts are likely to include:

- `ITypeParameterSymbol`;
- `INamedTypeSymbol`;
- `IMethodSymbol`;
- `ITypeSymbol`;
- `AttributeData`;
- `TypedConstant`;
- `SymbolEqualityComparer`;
- semantic type relationships;
- metadata symbols;
- overridden/interface member relationships.

Choose the exact analyzer registration strategy during planning and justify it from both correctness and performance perspectives.

---

# 57. Unresolved analyzer-behavior topics to discuss during planning

The following topics have **not yet been fully specified**.

Do not silently invent long-term semantics.

Surface them during the planning process, explain their impact, and propose a recommended v1 behavior.

Ask for a decision where it materially affects observable analyzer behavior.

Prefer keeping v1 small unless there is a strong consistency reason to support the feature immediately.

## 57.1 Generic lambdas

Determine whether generic lambda type parameters can/should support these attributes in v1.

Example conceptually:

```csharp
var f = <
    [DisallowTypes(typeof(Exception))]
    T>(T value) => value;
```

Confirm current C# syntax/API capabilities before proposing support.

---

## 57.2 Record types

Clarify whether generic:

- record classes;
- record structs;

are explicitly included in v1 or simply handled naturally through their class/struct symbols.

---

## 57.3 Nested generic types and containing type parameters

Clarify behavior for patterns such as:

```csharp
class Outer<T>
{
    class Inner
    {
        Data<T> Value;
    }
}
```

and:

```csharp
class Outer<T>
{
    class Inner<U>
    {
        Data<T> A;
        Data<U> B;
    }
}
```

Decide whether and how signature propagation crosses containing-type generic parameters in v1.

---

## 57.4 Generic parameter reordering during propagation

Clarify direct-mapping propagation for cases such as:

```csharp
interface IBase<T1, T2> { }

class Derived<A, B> :
    IBase<B, A>
{
}
```

When restrictions exist on one or both base parameters, define mapping precisely.

---

## 57.5 Repeated generic parameter mappings

Clarify behavior where the same generic parameter appears in multiple restricted positions.

Example:

```csharp
SomeType<T, T>
```

Define diagnostic de-duplication.

---

## 57.6 Explicit interface implementations and partial methods

Confirm precise propagation and diagnostic-location behavior for:

- explicit interface implementations;
- partial method declarations/implementations;
- overridden generic methods involving multiple metadata paths.

---

## 57.7 Additional semantic type-use locations

The broad rule is “check semantic uses of restricted constructed generics,” but enumerate what v1 will concretely cover.

Discuss cases such as:

- casts;
- `is` patterns;
- `as`;
- `default(TConstructed)`;
- `nameof`;
- target-typed `new`;
- collection expressions where constructed type comes from the target;
- attribute `typeof(...)`;
- constraints containing constructed generic types;
- pattern matching;
- tuple element types.

Avoid accidental syntax holes.

---

## 57.8 Pointer/function-pointer/unsafe type scenarios

Determine whether any valid attribute or generic scenarios involving:

- pointer types;
- function pointers;
- by-ref types;
- unsafe contexts;

need explicit behavior or can simply follow compiler limitations.

---

## 57.9 Error symbols and incomplete code

Define analyzer behavior when Roslyn returns:

- `IErrorTypeSymbol`;
- unresolved metadata;
- ambiguous binding;
- partially written code in the IDE.

The analyzer should avoid noisy or misleading diagnostics during broken/incomplete compilations.

---

## 57.10 Missing or malformed embedded attribute metadata

Determine how the analyzer behaves if a referenced assembly contains an attribute with the expected fully qualified metadata name but an unexpected constructor shape or malformed metadata.

Prefer defensive behavior.

---

## 57.11 Attribute identity across assemblies

Define precisely how the analyzer distinguishes:

- legitimate embedded `IvTem.TypeSafety` attributes;
- unrelated lookalike metadata with the same name;
- different package versions.

This is especially important because there is intentionally no shared runtime attribute assembly.

---

## 57.12 Diagnostic precedence

When a use site simultaneously has:

- a direct annotation violation;
- an inherited-contract violation;
- a signature-propagated violation;

define whether these collapse into one diagnostic and which declaration is cited in the message.

Avoid duplicate diagnostics.

---

## 57.13 Stable ordering of matched restrictions

If several forbidden types match, define deterministic ordering in the diagnostic.

Consider:

- declaration order;
- fully qualified metadata name;
- attribute/source order.

Choose and document one.

---

## 57.14 `null` attribute argument edge cases

Because `params Type[]` can produce subtle metadata shapes, explicitly test and define:

```csharp
[DisallowTypes(null)]
```

and any distinguishable “null array” versus “array containing null” representations accepted by C#.

---

## 57.15 Cross-language metadata

The analyzer supports only C# source in v1, but a C# project may reference an assembly produced by another .NET language.

Decide whether restrictions embedded in such metadata are supported, ignored, or simply handled when metadata has the expected contract.

Do not add Visual Basic source analysis unless explicitly approved.

---

## 57.16 Generated declarations from other generators

Generated source analysis is disabled in v1.

Clarify behavior when another generator creates a declaration carrying an `IvTem.TypeSafety` attribute and user-authored code later consumes that generated declaration.

Distinguish:

- analyzing generated declaration source;
- reading the resulting semantic contract from symbols;
- analyzing user-authored consumption.

---

## 57.17 Type forwarding and assembly identity

Consider whether type-forwarded framework/library types affect exact or assignability matching and whether Roslyn symbol equality already gives the required semantics.

Do not over-engineer unless testing reveals a problem.

---

## 57.18 Native integer aliases and other language aliases

Clarify whether aliases such as:

```csharp
int
System.Int32

nint
System.IntPtr
```

need explicit tests to verify semantic identity behavior.

Prefer semantic symbol identity over syntax spelling.

---

## 57.19 Diagnostic behavior for unsupported future scenarios

For explicitly deferred features such as aliases and reflection, decide whether v1 should:

- remain silent;
- produce a dedicated “unsupported analysis scenario” diagnostic;
- document only.

The current preference is generally to remain silent and document the limitation, but confirm during planning.

---

## 57.20 Scope boundaries of cycle detection

v1 rejects any cyclic generic-signature propagation graph.

During planning, define exactly which declarations participate in this graph so that an unrelated ordinary recursive type shape is not accidentally classified due to an overly broad implementation.

This area requires particular scrutiny because the chosen v1 behavior is intentionally conservative.

---

# 58. Planning output expected

Your first response/work session should produce a planning document containing:

1. repository/convention findings;
2. proposed solution/project structure;
3. proposed Roslyn architecture;
4. attribute-generation strategy;
5. analyzer registration strategy;
6. policy representation;
7. assignability algorithm;
8. exact-match algorithm;
9. propagation graph design;
10. cycle-detection design;
11. cross-assembly metadata design;
12. packaging/transitivity design;
13. complete proposed diagnostic catalog;
14. complete test strategy;
15. performance strategy;
16. documentation strategy;
17. sample strategy;
18. CI/package-validation strategy;
19. unresolved questions from Section 57;
20. small sequential implementation tasks with acceptance criteria;
21. risks and fallback approaches.

Where a behavioral choice remains unresolved, ask about it during planning instead of burying an assumption.

For behavioral questions, ask them clearly and preferably one at a time.

For repository, CI, packaging, Git, and versioning details not fixed by this specification, raise appropriate questions during planning.

---

# 59. Stop condition

After the planning artifacts have been created and the plan has been presented:

**STOP AND WAIT FOR APPROVAL.**

Do not begin Task 1 automatically.

Implementation begins only after explicit approval.