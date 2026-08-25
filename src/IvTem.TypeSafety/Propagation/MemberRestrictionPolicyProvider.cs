using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Propagation;

internal sealed class MemberRestrictionPolicyProvider
{
    private readonly DirectRestrictionPolicyExtractor extractor;

    public MemberRestrictionPolicyProvider(DirectRestrictionPolicyExtractor extractor)
    {
        this.extractor = extractor;
    }

    public ImmutableArray<RestrictionPolicy> GetMethodTypeParameterPolicies(
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        if (method.IsGenericMethod == false)
            return ImmutableArray<RestrictionPolicy>.Empty;

        var originalMethod = method.OriginalDefinition;
        var typeParameters = originalMethod.TypeParameters;
        if (typeParameters.Length == 0)
            return ImmutableArray<RestrictionPolicy>.Empty;

        var builders = new RestrictionPolicyBuilder[typeParameters.Length];
        for (var index = 0; index < typeParameters.Length; index++)
            builders[index] = new RestrictionPolicyBuilder(typeParameters[index]);

        foreach (var sourceMethod in GetContractSourceMethods(originalMethod, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceMethod.TypeParameters.Length != typeParameters.Length)
                continue;

            for (var index = 0; index < typeParameters.Length; index++)
            {
                var sourcePolicy = extractor.Extract(sourceMethod.TypeParameters[index], static _ => { }, cancellationToken);
                builders[index].Add(sourcePolicy);
            }
        }

        return builders
            .Select(builder => builder.ToPolicy())
            .ToImmutableArray();
    }

    private static IEnumerable<IMethodSymbol> GetContractSourceMethods(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var visitedMethods = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);

        foreach (var relatedMethod in GetPartialMethodParts(method))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (visitedMethods.Add(relatedMethod))
                yield return relatedMethod;
        }

        for (var current = method.OverriddenMethod?.OriginalDefinition; current is not null; current = current.OverriddenMethod?.OriginalDefinition)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var relatedMethod in GetPartialMethodParts(current))
            {
                if (visitedMethods.Add(relatedMethod))
                    yield return relatedMethod;
            }
        }

        foreach (var interfaceMethod in GetImplementedInterfaceMethods(method, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var relatedMethod in GetPartialMethodParts(interfaceMethod.OriginalDefinition))
            {
                if (visitedMethods.Add(relatedMethod))
                    yield return relatedMethod;
            }
        }
    }

    private static IEnumerable<IMethodSymbol> GetPartialMethodParts(IMethodSymbol method)
    {
        yield return method;

        if (method.PartialDefinitionPart is not null)
            yield return method.PartialDefinitionPart.OriginalDefinition;

        if (method.PartialImplementationPart is not null)
            yield return method.PartialImplementationPart.OriginalDefinition;
    }

    private static IEnumerable<IMethodSymbol> GetImplementedInterfaceMethods(IMethodSymbol method, CancellationToken cancellationToken)
    {
        foreach (var explicitImplementation in method.ExplicitInterfaceImplementations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return explicitImplementation.OriginalDefinition;
        }

        var containingType = method.ContainingType;
        if (containingType is null)
            yield break;

        foreach (var interfaceType in containingType.AllInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var member in interfaceType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (member is not IMethodSymbol interfaceMethod)
                    continue;

                if (interfaceMethod.IsGenericMethod == false)
                    continue;

                var implementation = containingType.FindImplementationForInterfaceMember(interfaceMethod);
                if (implementation is not IMethodSymbol implementationMethod)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(implementationMethod.OriginalDefinition, method) == false)
                    continue;

                yield return interfaceMethod.OriginalDefinition;
            }
        }
    }

    private sealed class RestrictionPolicyBuilder
    {
        private readonly ITypeParameterSymbol typeParameter;
        private readonly List<ForbiddenType> disallowAssignable = new();
        private readonly List<ForbiddenType> disallowExact = new();

        public RestrictionPolicyBuilder(ITypeParameterSymbol typeParameter)
        {
            this.typeParameter = typeParameter;
        }

        public void Add(RestrictionPolicy policy)
        {
            disallowAssignable.AddRange(policy.DisallowAssignable);
            disallowExact.AddRange(policy.DisallowExact);
        }

        public RestrictionPolicy ToPolicy()
            => new(
                typeParameter,
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
