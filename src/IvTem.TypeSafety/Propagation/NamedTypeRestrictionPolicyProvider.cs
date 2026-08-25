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
