using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Propagation;

internal sealed class NamedTypeRestrictionPolicyProvider
{
    private readonly DirectRestrictionPolicyExtractor extractor;
    private readonly ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<RestrictionPolicy>> policiesByType = new(SymbolEqualityComparer.Default);

    public NamedTypeRestrictionPolicyProvider(DirectRestrictionPolicyExtractor extractor)
    {
        this.extractor = extractor;
    }

    public ImmutableArray<RestrictionPolicy> GetTypeParameterPolicies(
        INamedTypeSymbol namedType,
        CancellationToken cancellationToken)
    {
        if (namedType.IsGenericType == false)
            return ImmutableArray<RestrictionPolicy>.Empty;

        return GetTypeParameterPoliciesCore(
            namedType.OriginalDefinition,
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default),
            cancellationToken);
    }

    private ImmutableArray<RestrictionPolicy> GetTypeParameterPoliciesCore(
        INamedTypeSymbol typeDefinition,
        HashSet<INamedTypeSymbol> recursionGuard,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (typeDefinition.TypeParameters.Length == 0)
            return ImmutableArray<RestrictionPolicy>.Empty;

        if (policiesByType.TryGetValue(typeDefinition, out var cachedPolicies))
            return cachedPolicies;

        if (recursionGuard.Add(typeDefinition) == false)
            return CreateDirectPolicies(typeDefinition, cancellationToken);

        var computedPolicies = CreatePolicies(typeDefinition, recursionGuard, cancellationToken);
        recursionGuard.Remove(typeDefinition);

        return policiesByType.GetOrAdd(typeDefinition, computedPolicies);
    }

    private ImmutableArray<RestrictionPolicy> CreatePolicies(
        INamedTypeSymbol typeDefinition,
        HashSet<INamedTypeSymbol> recursionGuard,
        CancellationToken cancellationToken)
    {
        var builders = CreatePolicyBuilders(typeDefinition.TypeParameters);

        foreach (var typeParameter in typeDefinition.TypeParameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            builders[typeParameter.Ordinal].Add(extractor.Extract(typeParameter, static _ => { }, cancellationToken));
        }

        foreach (var sourceType in GetBaseAndInterfaceTypes(typeDefinition, cancellationToken))
            AddMappedPolicies(sourceType, builders, recursionGuard, cancellationToken);

        foreach (var sourceType in GetSignatureTypes(typeDefinition, cancellationToken))
            AddMappedPolicies(sourceType, builders, recursionGuard, cancellationToken);

        return builders
            .Select(builder => builder.ToPolicy())
            .ToImmutableArray();
    }

    private void AddMappedPolicies(
        INamedTypeSymbol sourceType,
        RestrictionPolicyBuilder[] targetBuilders,
        HashSet<INamedTypeSymbol> recursionGuard,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sourcePolicies = GetTypeParameterPoliciesCore(sourceType.OriginalDefinition, recursionGuard, cancellationToken);
        if (sourceType.TypeArguments.Length != sourcePolicies.Length)
            return;

        for (var sourceIndex = 0; sourceIndex < sourceType.TypeArguments.Length; sourceIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceType.TypeArguments[sourceIndex] is not ITypeParameterSymbol targetTypeParameter)
                continue;

            if (targetTypeParameter.Ordinal < 0 || targetTypeParameter.Ordinal >= targetBuilders.Length)
                continue;

            if (SymbolEqualityComparer.Default.Equals(targetTypeParameter.ContainingSymbol, targetBuilders[targetTypeParameter.Ordinal].TypeParameter.ContainingSymbol) == false)
                continue;

            targetBuilders[targetTypeParameter.Ordinal].Add(sourcePolicies[sourceIndex]);
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetBaseAndInterfaceTypes(
        INamedTypeSymbol typeDefinition,
        CancellationToken cancellationToken)
    {
        for (var baseType = typeDefinition.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (baseType.IsGenericType)
                yield return baseType;
        }

        foreach (var interfaceType in typeDefinition.AllInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (interfaceType.IsGenericType)
                yield return interfaceType;
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetSignatureTypes(
        INamedTypeSymbol typeDefinition,
        CancellationToken cancellationToken)
    {
        foreach (var typeParameter in typeDefinition.TypeParameters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var constraintType in typeParameter.ConstraintTypes)
                foreach (var signatureType in FlattenSignatureType(constraintType, cancellationToken))
                    yield return signatureType;
        }

        foreach (var member in typeDefinition.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var signatureType in GetMemberSignatureTypes(member, cancellationToken))
                yield return signatureType;
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetMemberSignatureTypes(
        ISymbol member,
        CancellationToken cancellationToken)
    {
        switch (member)
        {
            case IFieldSymbol field:
                foreach (var signatureType in FlattenSignatureType(field.Type, cancellationToken))
                    yield return signatureType;

                break;

            case IPropertySymbol property:
                foreach (var signatureType in FlattenSignatureType(property.Type, cancellationToken))
                    yield return signatureType;

                foreach (var parameter in property.Parameters)
                    foreach (var signatureType in FlattenSignatureType(parameter.Type, cancellationToken))
                        yield return signatureType;

                break;

            case IEventSymbol eventSymbol:
                foreach (var signatureType in FlattenSignatureType(eventSymbol.Type, cancellationToken))
                    yield return signatureType;

                break;

            case IMethodSymbol method:
                if (IsSignatureMethod(method) == false)
                    break;

                foreach (var signatureType in FlattenSignatureType(method.ReturnType, cancellationToken))
                    yield return signatureType;

                foreach (var parameter in method.Parameters)
                    foreach (var signatureType in FlattenSignatureType(parameter.Type, cancellationToken))
                        yield return signatureType;

                foreach (var typeParameter in method.TypeParameters)
                    foreach (var constraintType in typeParameter.ConstraintTypes)
                        foreach (var signatureType in FlattenSignatureType(constraintType, cancellationToken))
                            yield return signatureType;

                break;
        }
    }

    private static IEnumerable<INamedTypeSymbol> FlattenSignatureType(ITypeSymbol type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (type)
        {
            case INamedTypeSymbol namedType:
                if (namedType.IsGenericType)
                    yield return namedType;

                foreach (var typeArgument in namedType.TypeArguments)
                    foreach (var signatureType in FlattenSignatureType(typeArgument, cancellationToken))
                        yield return signatureType;

                break;

            case IArrayTypeSymbol arrayType:
                foreach (var signatureType in FlattenSignatureType(arrayType.ElementType, cancellationToken))
                    yield return signatureType;

                break;

            case IPointerTypeSymbol pointerType:
                foreach (var signatureType in FlattenSignatureType(pointerType.PointedAtType, cancellationToken))
                    yield return signatureType;

                break;
        }
    }

    private static bool IsSignatureMethod(IMethodSymbol method)
        => method.MethodKind is not MethodKind.PropertyGet
            and not MethodKind.PropertySet
            and not MethodKind.EventAdd
            and not MethodKind.EventRemove
            and not MethodKind.EventRaise;

    private static RestrictionPolicyBuilder[] CreatePolicyBuilders(ImmutableArray<ITypeParameterSymbol> typeParameters)
    {
        var builders = new RestrictionPolicyBuilder[typeParameters.Length];

        for (var index = 0; index < typeParameters.Length; index++)
            builders[index] = new RestrictionPolicyBuilder(typeParameters[index]);

        return builders;
    }

    private ImmutableArray<RestrictionPolicy> CreateDirectPolicies(
        INamedTypeSymbol typeDefinition,
        CancellationToken cancellationToken)
        => typeDefinition.TypeParameters
            .Select(typeParameter => extractor.Extract(typeParameter, static _ => { }, cancellationToken))
            .ToImmutableArray();

    private sealed class RestrictionPolicyBuilder
    {
        private readonly List<ForbiddenType> disallowAssignable = new();
        private readonly List<ForbiddenType> disallowExact = new();

        public RestrictionPolicyBuilder(ITypeParameterSymbol typeParameter)
        {
            TypeParameter = typeParameter;
        }

        public ITypeParameterSymbol TypeParameter { get; }

        public void Add(RestrictionPolicy policy)
        {
            disallowAssignable.AddRange(policy.DisallowAssignable);
            disallowExact.AddRange(policy.DisallowExact);
        }

        public RestrictionPolicy ToPolicy()
            => new(
                TypeParameter,
                Deduplicate(disallowAssignable),
                Deduplicate(disallowExact));

        private static ImmutableArray<ForbiddenType> Deduplicate(IEnumerable<ForbiddenType> forbiddenTypes)
        {
            var byType = new Dictionary<ITypeSymbol, ForbiddenType>(SymbolEqualityComparer.Default);

            foreach (var forbiddenType in forbiddenTypes)
            {
                if (byType.ContainsKey(forbiddenType.Type) == false)
                    byType.Add(forbiddenType.Type, forbiddenType);
            }

            return byType.Values.ToImmutableArray();
        }
    }
}
