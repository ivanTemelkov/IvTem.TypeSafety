using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Policies;

internal sealed class ExactTypeMatcher
{
    public ExactTypeMatcher(Compilation compilation)
    {
        Compilation = compilation;
    }

    private Compilation Compilation { get; }

    public bool Matches(ITypeSymbol actualType, ITypeSymbol forbiddenType)
    {
        var normalizedActualType = Normalize(actualType);
        var normalizedForbiddenType = Normalize(forbiddenType);

        return SymbolEqualityComparer.Default.Equals(normalizedActualType, normalizedForbiddenType);
    }

    private ITypeSymbol Normalize(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Dynamic)
            return Compilation.GetSpecialType(SpecialType.System_Object);

        return type;
    }
}
