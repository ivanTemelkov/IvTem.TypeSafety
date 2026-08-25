using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Diagnostics;

internal static class TypeSafetyDiagnosticDescriptors
{
    public const string ForbiddenGenericArgumentId = "IVTS001";
    public const string InvalidConfigurationId = "IVTS002";
    public const string CyclicContractPropagationId = "IVTS003";
    public const string ContradictoryRestrictionId = "IVTS004";
    public const string MalformedAttributeMetadataId = "IVTS005";

    private const string Category = "TypeSafety";

    public static readonly DiagnosticDescriptor ForbiddenGenericArgument = new(
        ForbiddenGenericArgumentId,
        "Forbidden generic argument",
        "Type argument '{0}' is not allowed for generic parameter '{1}' of '{2}'. Matched restriction(s): {3}.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidConfiguration = new(
        InvalidConfigurationId,
        "Invalid type-safety attribute configuration",
        "Invalid {0} configuration on generic parameter '{1}': {2}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CyclicContractPropagation = new(
        CyclicContractPropagationId,
        "Cyclic type-safety contract propagation",
        "Cyclic generic type-safety contract propagation detected among: {0}. Cycles are not supported in v1.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContradictoryRestriction = new(
        ContradictoryRestrictionId,
        "Contradictory generic parameter restriction",
        "Generic parameter '{0}' has a type-safety restriction that contradicts its direct constraints: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MalformedAttributeMetadata = new(
        MalformedAttributeMetadataId,
        "Malformed type-safety attribute metadata",
        "Attribute '{0}' has the IvTem.TypeSafety metadata name but does not match the expected v1 contract: {1}",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly ImmutableArray<DiagnosticDescriptor> All =
    [
        ForbiddenGenericArgument,
        InvalidConfiguration,
        CyclicContractPropagation,
        ContradictoryRestriction,
        MalformedAttributeMetadata
    ];
}
