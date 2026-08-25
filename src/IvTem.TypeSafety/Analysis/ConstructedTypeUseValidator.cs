using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using IvTem.TypeSafety.Diagnostics;
using IvTem.TypeSafety.Policies;
using IvTem.TypeSafety.Propagation;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Analysis;

internal sealed class ConstructedTypeUseValidator
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    private readonly MemberRestrictionPolicyProvider memberRestrictionPolicyProvider;
    private readonly NamedTypeRestrictionPolicyProvider namedTypeRestrictionPolicyProvider;
    private readonly ExactTypeMatcher exactTypeMatcher;
    private readonly AssignableTypeMatcher assignableTypeMatcher;
    private readonly DiagnosticDeduplicator diagnosticDeduplicator;

    public ConstructedTypeUseValidator(
        MemberRestrictionPolicyProvider memberRestrictionPolicyProvider,
        NamedTypeRestrictionPolicyProvider namedTypeRestrictionPolicyProvider,
        ExactTypeMatcher exactTypeMatcher,
        AssignableTypeMatcher assignableTypeMatcher,
        DiagnosticDeduplicator diagnosticDeduplicator)
    {
        this.memberRestrictionPolicyProvider = memberRestrictionPolicyProvider;
        this.namedTypeRestrictionPolicyProvider = namedTypeRestrictionPolicyProvider;
        this.exactTypeMatcher = exactTypeMatcher;
        this.assignableTypeMatcher = assignableTypeMatcher;
        this.diagnosticDeduplicator = diagnosticDeduplicator;
    }

    public void Validate(
        INamedTypeSymbol namedType,
        ImmutableArray<Location> typeArgumentLocations,
        Location fallbackLocation,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        if (namedType.IsUnboundGenericType)
            return;

        var typeArguments = namedType.TypeArguments;
        var policies = namedTypeRestrictionPolicyProvider.GetTypeParameterPolicies(namedType, cancellationToken);
        if (typeArguments.Length != policies.Length)
            return;

        ValidateCore(
            typeArguments,
            policies,
            namedType.OriginalDefinition.ToDisplayString(TypeDisplayFormat),
            typeArgumentLocations,
            fallbackLocation,
            reportDiagnostic,
            cancellationToken);
    }

    public void Validate(
        IMethodSymbol method,
        ImmutableArray<Location> typeArgumentLocations,
        Location fallbackLocation,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        if (method.IsGenericMethod == false)
            return;

        var typeArguments = method.TypeArguments;
        var policies = memberRestrictionPolicyProvider.GetMethodTypeParameterPolicies(method, cancellationToken);
        if (typeArguments.Length != policies.Length)
            return;

        ValidateCore(
            typeArguments,
            policies,
            method.OriginalDefinition.ToDisplayString(TypeDisplayFormat),
            typeArgumentLocations,
            fallbackLocation,
            reportDiagnostic,
            cancellationToken);
    }

    private void ValidateCore(
        ImmutableArray<ITypeSymbol> typeArguments,
        ImmutableArray<RestrictionPolicy> policies,
        string originalDefinitionDisplayName,
        ImmutableArray<Location> typeArgumentLocations,
        Location fallbackLocation,
        Action<Diagnostic> reportDiagnostic,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < typeArguments.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var actualType = typeArguments[index];
            if (ContainsErrorType(actualType))
                continue;

            var policy = policies[index];
            var matchedRestrictions = GetMatchedRestrictions(actualType, policy);
            if (matchedRestrictions.Length == 0)
                continue;

            var location = GetTypeArgumentLocation(typeArgumentLocations, fallbackLocation, index);
            if (diagnosticDeduplicator.TryMarkReported(location, index) == false)
                continue;

            reportDiagnostic(Diagnostic.Create(
                TypeSafetyDiagnosticDescriptors.ForbiddenGenericArgument,
                location,
                actualType.ToDisplayString(TypeDisplayFormat),
                policy.TypeParameter.Name,
                originalDefinitionDisplayName,
                FormatMatchedRestrictions(matchedRestrictions)));
        }
    }

    private ImmutableArray<string> GetMatchedRestrictions(ITypeSymbol actualType, RestrictionPolicy policy)
        => policy.DisallowAssignable
            .Where(forbiddenType => assignableTypeMatcher.Matches(actualType, forbiddenType.Type))
            .Select(forbiddenType => FormatRestriction("DisallowTypes", forbiddenType))
            .Concat(policy.DisallowExact
                .Where(forbiddenType => exactTypeMatcher.Matches(actualType, forbiddenType.Type))
                .Select(forbiddenType => FormatRestriction("DisallowExactTypes", forbiddenType)))
            .ToImmutableArray();

    private static Location GetTypeArgumentLocation(ImmutableArray<Location> typeArgumentLocations, Location fallbackLocation, int index)
    {
        if (typeArgumentLocations.IsDefaultOrEmpty)
            return fallbackLocation;

        if (typeArgumentLocations.Length <= index)
            return fallbackLocation;

        var location = typeArgumentLocations[index];
        return location == Location.None
            ? fallbackLocation
            : location;
    }

    private static bool ContainsErrorType(ITypeSymbol type)
        => type switch
        {
            IErrorTypeSymbol => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsErrorType),
            IArrayTypeSymbol arrayType => ContainsErrorType(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsErrorType(pointerType.PointedAtType),
            _ => false
        };

    private static string FormatRestriction(string attributeName, ForbiddenType forbiddenType)
        => attributeName + "(" + forbiddenType.DisplayName + ")";

    private static string FormatMatchedRestrictions(ImmutableArray<string> matchedRestrictions)
        => string.Join(", ", matchedRestrictions);
}
