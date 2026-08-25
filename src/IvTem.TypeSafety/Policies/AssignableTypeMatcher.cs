using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IvTem.TypeSafety.Policies;

internal sealed class AssignableTypeMatcher
{
    public AssignableTypeMatcher(Compilation compilation)
    {
        Compilation = compilation;
    }

    private Compilation Compilation { get; }

    public bool Matches(ITypeSymbol actualType, ITypeSymbol forbiddenType)
    {
        var normalizedActualType = Normalize(actualType);
        var normalizedForbiddenType = Normalize(forbiddenType);

        if (normalizedActualType is ITypeParameterSymbol typeParameter)
            return MatchesDirectConstraint(typeParameter, normalizedForbiddenType);

        return IsDefiniteAssignable(normalizedActualType, normalizedForbiddenType);
    }

    private bool MatchesDirectConstraint(ITypeParameterSymbol typeParameter, ITypeSymbol forbiddenType)
    {
        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            if (constraintType is ITypeParameterSymbol)
                continue;

            if (IsDefiniteAssignable(Normalize(constraintType), forbiddenType))
                return true;
        }

        return false;
    }

    private bool IsDefiniteAssignable(ITypeSymbol actualType, ITypeSymbol forbiddenType)
    {
        var conversion = Compilation.ClassifyConversion(actualType, forbiddenType);

        if (conversion.IsImplicit
            && (conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing))
            return true;

        return HasImplementedInterfaceRelationship(actualType, forbiddenType);
    }

    private bool HasImplementedInterfaceRelationship(ITypeSymbol actualType, ITypeSymbol forbiddenType)
    {
        if (actualType is not INamedTypeSymbol namedType)
            return false;

        if (forbiddenType.TypeKind != TypeKind.Interface)
            return false;

        foreach (var implementedInterface in namedType.AllInterfaces)
        {
            if (IsDefiniteAssignable(implementedInterface, forbiddenType))
                return true;
        }

        return false;
    }

    private ITypeSymbol Normalize(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Dynamic)
            return Compilation.GetSpecialType(SpecialType.System_Object);

        return type;
    }
}
