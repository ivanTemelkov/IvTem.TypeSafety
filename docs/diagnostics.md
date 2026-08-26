# Diagnostics

All `IvTem.TypeSafety` diagnostics are errors by default and use the `TypeSafety` category.

## IVTS001 Forbidden Generic Argument

`IVTS001` is reported when a constructed generic type or generic method supplies a type argument that matches a `DisallowTypes` or `DisallowExactTypes` restriction.

```csharp
using IvTem.TypeSafety;

public sealed class Result<[DisallowTypes(typeof(System.Exception))] T>
{
}

Result<System.InvalidOperationException> value = new(); // IVTS001
```

Fix the diagnostic by changing the type argument or relaxing the restriction on the generic declaration. The diagnostic message lists the matched restriction names, including propagated restrictions.

## IVTS002 Invalid Type-Safety Attribute Configuration

`IVTS002` is reported when a restriction attribute has a valid v1 shape but invalid configuration values.

Invalid configurations include:

- an empty type list;
- a null type list or null entry;
- an open or unbound generic type such as `IEnumerable<>`;
- a forbidden type that contains a generic parameter;
- `DisallowTypes(typeof(object))`.

```csharp
using IvTem.TypeSafety;

public sealed class Invalid<[DisallowTypes(typeof(object))] T>
{
}
```

Use closed, concrete forbidden types. `DisallowExactTypes(typeof(object))` is allowed because exact object matching is narrower than assignable-to-object matching.

## IVTS003 Cyclic Type-Safety Contract Propagation

`IVTS003` is reported when generic signature propagation forms a cycle.

```csharp
using IvTem.TypeSafety;

public sealed class First<T>
{
    private Second<T>? Value { get; set; }
}

public sealed class Second<T>
{
    private First<T>? Value { get; set; }
}
```

Cycles are rejected in v1 because the analyzer does not compute fixed-point restriction contracts. Break the cycle by removing the direct generic-parameter mapping from one declaration signature.

## IVTS004 Contradictory Generic Parameter Restriction

`IVTS004` is present in the descriptor catalog for the planned contradictory-restriction diagnostic:

```text
Generic parameter '{0}' has a type-safety restriction that contradicts its direct constraints: {1}
```

No current `0.1.1` analyzer path emits `IVTS004`. It remains reserved so the diagnostic ID does not need to change if contradictory direct-constraint validation is completed later.

## IVTS005 Malformed Type-Safety Attribute Metadata

`IVTS005` is reported when current source defines or uses an attribute with the owned `IvTem.TypeSafety` metadata name but the type does not match the expected v1 contract.

```csharp
namespace IvTem.TypeSafety;

public sealed class DisallowTypesAttribute : System.Attribute
{
    public DisallowTypesAttribute(string value)
    {
    }
}
```

Do not hand-author lookalike attributes in the `IvTem.TypeSafety` namespace. Let the package source generator provide the attributes.
