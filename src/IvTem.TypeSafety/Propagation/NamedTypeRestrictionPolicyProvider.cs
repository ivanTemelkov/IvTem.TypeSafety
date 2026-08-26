using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using IvTem.TypeSafety.Diagnostics;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Propagation;

internal sealed class NamedTypeRestrictionPolicyProvider
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private readonly DirectRestrictionPolicyExtractor extractor;
    private readonly ConcurrentDictionary<INamedTypeSymbol, ImmutableArray<RestrictionPolicy>> policiesByType = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<INamedTypeSymbol, byte> cycleAnalysisByType = new(SymbolEqualityComparer.Default);
    private readonly ConcurrentDictionary<PropagationNode, byte> cyclicNodes = new(new PropagationNodeComparer());
    private readonly ConcurrentDictionary<string, byte> reportedCycles = new(StringComparer.Ordinal);
    private readonly object cycleAnalysisGate = new();

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

        EnsureCyclesAnalyzed(namedType.OriginalDefinition, cancellationToken);

        return GetTypeParameterPoliciesCore(
            namedType.OriginalDefinition,
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default),
            cancellationToken);
    }

    public void ReportCyclicContractPropagation(
        INamedTypeSymbol namedType,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        if (namedType.TypeParameters.Length == 0)
            return;

        foreach (var component in GetCycleComponents(namedType.OriginalDefinition, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var orderedComponent = OrderNodes(component).ToImmutableArray();
            var cycleKey = FormatCycleKey(orderedComponent);
            if (reportedCycles.TryAdd(cycleKey, 0) == false)
                continue;

            reportDiagnostic(Diagnostic.Create(
                TypeSafetyDiagnosticDescriptors.CyclicContractPropagation,
                GetDiagnosticLocation(orderedComponent),
                FormatCycleDisplay(orderedComponent)));
        }
    }

    private ImmutableArray<RestrictionPolicy> GetTypeParameterPoliciesCore(
        INamedTypeSymbol typeDefinition,
        HashSet<INamedTypeSymbol> recursionGuard,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (typeDefinition.TypeParameters.Length == 0)
            return ImmutableArray<RestrictionPolicy>.Empty;

        EnsureCyclesAnalyzed(typeDefinition, cancellationToken);

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

            if (cyclicNodes.ContainsKey(new PropagationNode(sourceType.OriginalDefinition, sourceIndex)))
                continue;

            if (sourceType.TypeArguments[sourceIndex] is not ITypeParameterSymbol targetTypeParameter)
                continue;

            if (targetTypeParameter.Ordinal < 0 || targetTypeParameter.Ordinal >= targetBuilders.Length)
                continue;

            if (SymbolEqualityComparer.Default.Equals(targetTypeParameter.ContainingSymbol, targetBuilders[targetTypeParameter.Ordinal].TypeParameter.ContainingSymbol) == false)
                continue;

            if (IsIdentitySelfMapping(sourceType, sourceIndex, targetTypeParameter))
                continue;

            targetBuilders[targetTypeParameter.Ordinal].Add(sourcePolicies[sourceIndex]);
        }
    }

    private void EnsureCyclesAnalyzed(INamedTypeSymbol typeDefinition, CancellationToken cancellationToken)
    {
        lock (cycleAnalysisGate)
        {
            if (cycleAnalysisByType.ContainsKey(typeDefinition))
                return;

            foreach (var component in GetCycleComponents(typeDefinition, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var node in component)
                    _ = cyclicNodes.TryAdd(node, 0);
            }

            _ = cycleAnalysisByType.TryAdd(typeDefinition, 0);
        }
    }

    private ImmutableArray<ImmutableArray<PropagationNode>> GetCycleComponents(
        INamedTypeSymbol rootTypeDefinition,
        CancellationToken cancellationToken)
    {
        var graph = BuildReachableGraph(rootTypeDefinition, cancellationToken);
        var state = new StronglyConnectedComponentState();

        foreach (var node in graph.Keys.OrderBy(FormatNodeKey, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (state.Indexes.ContainsKey(node) == false)
                FindStronglyConnectedComponents(node, graph, state, cancellationToken);
        }

        return state.Components
            .Where(component => IsCycle(component, graph))
            .Select(component => OrderNodes(component).ToImmutableArray())
            .OrderBy(component => FormatCycleKey(component), StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static Dictionary<PropagationNode, ImmutableArray<PropagationNode>> BuildReachableGraph(
        INamedTypeSymbol rootTypeDefinition,
        CancellationToken cancellationToken)
    {
        var graph = new Dictionary<PropagationNode, ImmutableArray<PropagationNode>>(new PropagationNodeComparer());
        var pending = new Stack<PropagationNode>();

        foreach (var node in CreateTypeParameterNodes(rootTypeDefinition))
            pending.Push(node);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = pending.Pop();
            if (graph.ContainsKey(node))
                continue;

            var edges = GetPropagationEdges(node, cancellationToken)
                .OrderBy(FormatNodeKey, StringComparer.Ordinal)
                .ToImmutableArray();

            graph.Add(node, edges);

            foreach (var edge in edges)
            {
                if (graph.ContainsKey(edge) == false)
                    pending.Push(edge);
            }
        }

        return graph;
    }

    private static void FindStronglyConnectedComponents(
        PropagationNode node,
        Dictionary<PropagationNode, ImmutableArray<PropagationNode>> graph,
        StronglyConnectedComponentState state,
        CancellationToken cancellationToken)
    {
        state.Indexes.Add(node, state.NextIndex);
        state.LowLinks.Add(node, state.NextIndex);
        state.NextIndex++;
        state.Stack.Push(node);
        state.NodesOnStack.Add(node);

        foreach (var edge in graph[node])
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (state.Indexes.ContainsKey(edge) == false)
            {
                FindStronglyConnectedComponents(edge, graph, state, cancellationToken);
                state.LowLinks[node] = Math.Min(state.LowLinks[node], state.LowLinks[edge]);
                continue;
            }

            if (state.NodesOnStack.Contains(edge))
                state.LowLinks[node] = Math.Min(state.LowLinks[node], state.Indexes[edge]);
        }

        if (state.LowLinks[node] != state.Indexes[node])
            return;

        var component = ImmutableArray.CreateBuilder<PropagationNode>();
        PropagationNode componentNode;

        do
        {
            componentNode = state.Stack.Pop();
            state.NodesOnStack.Remove(componentNode);
            component.Add(componentNode);
        }
        while (new PropagationNodeComparer().Equals(componentNode, node) == false);

        state.Components.Add(component.ToImmutable());
    }

    private static bool IsCycle(
        ImmutableArray<PropagationNode> component,
        Dictionary<PropagationNode, ImmutableArray<PropagationNode>> graph)
    {
        if (component.Length > 1)
            return true;

        var node = component[0];
        return graph[node].Contains(node, new PropagationNodeComparer());
    }

    private static IEnumerable<PropagationNode> GetPropagationEdges(
        PropagationNode node,
        CancellationToken cancellationToken)
    {
        foreach (var sourceType in GetBaseAndInterfaceTypes(node.TypeDefinition, cancellationToken))
            foreach (var edge in GetMappedEdges(node, sourceType, cancellationToken))
                yield return edge;

        foreach (var sourceType in GetSignatureTypes(node.TypeDefinition, cancellationToken))
            foreach (var edge in GetMappedEdges(node, sourceType, cancellationToken))
                yield return edge;
    }

    private static IEnumerable<PropagationNode> GetMappedEdges(
        PropagationNode targetNode,
        INamedTypeSymbol sourceType,
        CancellationToken cancellationToken)
    {
        for (var sourceIndex = 0; sourceIndex < sourceType.TypeArguments.Length; sourceIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceType.TypeArguments[sourceIndex] is not ITypeParameterSymbol targetTypeParameter)
                continue;

            if (targetTypeParameter.Ordinal != targetNode.TypeParameterOrdinal)
                continue;

            if (SymbolEqualityComparer.Default.Equals(targetTypeParameter.ContainingSymbol, targetNode.TypeDefinition) == false)
                continue;

            if (IsIdentitySelfMapping(sourceType, sourceIndex, targetTypeParameter))
                continue;

            yield return new PropagationNode(sourceType.OriginalDefinition, sourceIndex);
        }
    }

    private static bool IsIdentitySelfMapping(
        INamedTypeSymbol sourceType,
        int sourceTypeArgumentIndex,
        ITypeParameterSymbol targetTypeParameter)
        => targetTypeParameter.Ordinal == sourceTypeArgumentIndex
            && SymbolEqualityComparer.Default.Equals(sourceType.OriginalDefinition, targetTypeParameter.ContainingSymbol);

    private static IEnumerable<PropagationNode> CreateTypeParameterNodes(INamedTypeSymbol typeDefinition)
    {
        for (var ordinal = 0; ordinal < typeDefinition.TypeParameters.Length; ordinal++)
            yield return new PropagationNode(typeDefinition, ordinal);
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

    private static Location GetDiagnosticLocation(ImmutableArray<PropagationNode> component)
        => component
            .Select(GetTypeParameterLocation)
            .Where(location => location != Location.None)
            .OrderBy(GetLocationSortKey, StringComparer.Ordinal)
            .FirstOrDefault() ?? Location.None;

    private static Location GetTypeParameterLocation(PropagationNode node)
    {
        if (node.TypeParameterOrdinal < 0 || node.TypeParameterOrdinal >= node.TypeDefinition.TypeParameters.Length)
            return Location.None;

        return node.TypeDefinition.TypeParameters[node.TypeParameterOrdinal]
            .Locations
            .Where(location => location.IsInSource)
            .OrderBy(GetLocationSortKey, StringComparer.Ordinal)
            .FirstOrDefault() ?? Location.None;
    }

    private static string GetLocationSortKey(Location location)
    {
        var sourceTreePath = location.SourceTree?.FilePath ?? string.Empty;
        return sourceTreePath + ":" + location.SourceSpan.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<PropagationNode> OrderNodes(IEnumerable<PropagationNode> nodes)
        => nodes.OrderBy(FormatNodeKey, StringComparer.Ordinal);

    private static string FormatCycleDisplay(IEnumerable<PropagationNode> nodes)
        => string.Join(", ", OrderNodes(nodes).Select(FormatNodeDisplay));

    private static string FormatCycleKey(IEnumerable<PropagationNode> nodes)
        => string.Join("|", OrderNodes(nodes).Select(FormatNodeKey));

    private static string FormatNodeDisplay(PropagationNode node)
        => node.TypeDefinition.ToDisplayString(TypeDisplayFormat) + "." + node.TypeDefinition.TypeParameters[node.TypeParameterOrdinal].Name;

    private static string FormatNodeKey(PropagationNode node)
        => node.TypeDefinition.ToDisplayString(TypeDisplayFormat)
            + "#"
            + node.TypeParameterOrdinal.ToString(System.Globalization.CultureInfo.InvariantCulture);

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

    private sealed class PropagationNode
    {
        public PropagationNode(INamedTypeSymbol typeDefinition, int typeParameterOrdinal)
        {
            TypeDefinition = typeDefinition;
            TypeParameterOrdinal = typeParameterOrdinal;
        }

        public INamedTypeSymbol TypeDefinition { get; }

        public int TypeParameterOrdinal { get; }
    }

    private sealed class PropagationNodeComparer : IEqualityComparer<PropagationNode>
    {
        public bool Equals(PropagationNode? x, PropagationNode? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.TypeParameterOrdinal == y.TypeParameterOrdinal
                && SymbolEqualityComparer.Default.Equals(x.TypeDefinition, y.TypeDefinition);
        }

        public int GetHashCode(PropagationNode obj)
        {
            var hash = 17;
            hash = (hash * 31) + SymbolEqualityComparer.Default.GetHashCode(obj.TypeDefinition);
            hash = (hash * 31) + obj.TypeParameterOrdinal.GetHashCode();
            return hash;
        }
    }

    private sealed class StronglyConnectedComponentState
    {
        public Dictionary<PropagationNode, int> Indexes { get; } = new(new PropagationNodeComparer());

        public Dictionary<PropagationNode, int> LowLinks { get; } = new(new PropagationNodeComparer());

        public Stack<PropagationNode> Stack { get; } = new();

        public HashSet<PropagationNode> NodesOnStack { get; } = new(new PropagationNodeComparer());

        public List<ImmutableArray<PropagationNode>> Components { get; } = new();

        public int NextIndex { get; set; }
    }

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
