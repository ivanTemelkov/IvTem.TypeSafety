using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Policies;

internal sealed class RestrictionPolicy
{
    public RestrictionPolicy(
        ITypeParameterSymbol typeParameter,
        ImmutableArray<ForbiddenType> disallowAssignable,
        ImmutableArray<ForbiddenType> disallowExact)
    {
        TypeParameter = typeParameter;
        DisallowAssignable = disallowAssignable;
        DisallowExact = disallowExact;
    }

    public ITypeParameterSymbol TypeParameter { get; }

    public ImmutableArray<ForbiddenType> DisallowAssignable { get; }

    public ImmutableArray<ForbiddenType> DisallowExact { get; }
}
