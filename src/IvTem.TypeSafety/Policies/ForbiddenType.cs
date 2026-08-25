using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Policies;

internal sealed class ForbiddenType
{
    public ForbiddenType(ITypeSymbol type, string displayName, Location location, int declarationOrder)
    {
        Type = type;
        DisplayName = displayName;
        Location = location;
        DeclarationOrder = declarationOrder;
    }

    public ITypeSymbol Type { get; }

    public string DisplayName { get; }

    public Location Location { get; }

    public int DeclarationOrder { get; }
}
