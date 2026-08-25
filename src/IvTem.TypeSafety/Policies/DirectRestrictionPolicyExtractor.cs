using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using IvTem.TypeSafety.Diagnostics;
using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Policies;

internal sealed class DirectRestrictionPolicyExtractor
{
    private const string DisallowTypesMetadataName = "IvTem.TypeSafety.DisallowTypesAttribute";
    private const string DisallowExactTypesMetadataName = "IvTem.TypeSafety.DisallowExactTypesAttribute";

    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public RestrictionPolicy Extract(ITypeParameterSymbol typeParameter, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
    {
        var assignableTypes = new List<ForbiddenType>();
        var exactTypes = new List<ForbiddenType>();
        var declarationOrder = 0;

        foreach (var attribute in typeParameter.GetAttributes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var restrictionKind = GetRestrictionKind(attribute);
            if (restrictionKind is null)
                continue;

            if (HasExpectedAttributeShape(attribute.AttributeClass) == false)
            {
                reportDiagnostic(CreateMalformedAttributeDiagnostic(
                    attribute,
                    typeParameter,
                    "expected an attribute deriving from System.Attribute with one params System.Type[] constructor parameter",
                    cancellationToken));
                continue;
            }

            var target = restrictionKind.Value == RestrictionKind.Assignable
                ? assignableTypes
                : exactTypes;

            ExtractAttributeTypes(attribute, typeParameter, restrictionKind.Value, target, reportDiagnostic, ref declarationOrder, cancellationToken);
        }

        return new RestrictionPolicy(
            typeParameter,
            Deduplicate(assignableTypes),
            Deduplicate(exactTypes));
    }

    private static RestrictionKind? GetRestrictionKind(AttributeData attribute)
        => GetAttributeMetadataName(attribute.AttributeClass) switch
        {
            DisallowTypesMetadataName => RestrictionKind.Assignable,
            DisallowExactTypesMetadataName => RestrictionKind.Exact,
            _ => null
        };

    private static string? GetAttributeMetadataName(INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
            return null;

        var namespaceName = attributeType.ContainingNamespace.ToDisplayString();
        if (string.IsNullOrEmpty(namespaceName))
            return attributeType.MetadataName;

        return namespaceName + "." + attributeType.MetadataName;
    }

    private bool HasExpectedAttributeShape(INamedTypeSymbol? attributeType)
    {
        if (attributeType is null)
            return false;

        if (DerivesFromSystemAttribute(attributeType) == false)
            return false;

        var constructor = attributeType.InstanceConstructors
            .FirstOrDefault(candidate =>
                candidate.Parameters.Length == 1
                && candidate.Parameters[0].IsParams
                && IsSystemTypeArray(candidate.Parameters[0].Type));

        return constructor is not null;
    }

    private bool DerivesFromSystemAttribute(INamedTypeSymbol attributeType)
    {
        for (var current = attributeType; current is not null; current = current.BaseType)
        {
            if (GetAttributeMetadataName(current) == "System.Attribute")
                return true;
        }

        return false;
    }

    private bool IsSystemTypeArray(ITypeSymbol type)
        => type is IArrayTypeSymbol arrayType
            && arrayType.ElementType is INamedTypeSymbol elementType
            && GetAttributeMetadataName(elementType) == "System.Type";

    private void ExtractAttributeTypes(
        AttributeData attribute,
        ITypeParameterSymbol typeParameter,
        RestrictionKind restrictionKind,
        List<ForbiddenType> target,
        Action<Diagnostic> reportDiagnostic,
        ref int declarationOrder,
        CancellationToken cancellationToken)
    {
        if (attribute.ConstructorArguments.Length != 1)
        {
            reportDiagnostic(CreateMalformedAttributeDiagnostic(
                attribute,
                typeParameter,
                "expected exactly one constructor argument",
                cancellationToken));
            return;
        }

        var argument = attribute.ConstructorArguments[0];
        if (argument.IsNull)
        {
            reportDiagnostic(CreateInvalidConfigurationDiagnostic(
                attribute,
                typeParameter,
                restrictionKind,
                "the type list is null",
                cancellationToken));
            return;
        }

        if (argument.Kind != TypedConstantKind.Array)
        {
            reportDiagnostic(CreateMalformedAttributeDiagnostic(
                attribute,
                typeParameter,
                "expected a System.Type[] constructor argument",
                cancellationToken));
            return;
        }

        if (argument.Values.Length == 0)
        {
            reportDiagnostic(CreateInvalidConfigurationDiagnostic(
                attribute,
                typeParameter,
                restrictionKind,
                "the type list is empty",
                cancellationToken));
            return;
        }

        foreach (var entry in argument.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            declarationOrder++;

            if (entry.IsNull)
            {
                reportDiagnostic(CreateInvalidConfigurationDiagnostic(
                    attribute,
                    typeParameter,
                    restrictionKind,
                    "the type list contains a null entry",
                    cancellationToken));
                continue;
            }

            if (entry.Kind != TypedConstantKind.Type || entry.Value is not ITypeSymbol type)
            {
                reportDiagnostic(CreateInvalidConfigurationDiagnostic(
                    attribute,
                    typeParameter,
                    restrictionKind,
                    "each entry must be a System.Type value",
                    cancellationToken));
                continue;
            }

            var invalidReason = GetInvalidForbiddenTypeReason(type, restrictionKind);
            if (invalidReason is not null)
            {
                reportDiagnostic(CreateInvalidConfigurationDiagnostic(
                    attribute,
                    typeParameter,
                    restrictionKind,
                    invalidReason,
                    cancellationToken));
                continue;
            }

            if (ContainsErrorType(type))
                continue;

            target.Add(new ForbiddenType(
                type,
                type.ToDisplayString(TypeDisplayFormat),
                GetAttributeLocation(attribute, typeParameter, cancellationToken),
                declarationOrder));
        }
    }

    private static ImmutableArray<ForbiddenType> Deduplicate(List<ForbiddenType> forbiddenTypes)
    {
        var byType = new Dictionary<ITypeSymbol, ForbiddenType>(SymbolEqualityComparer.Default);

        foreach (var forbiddenType in forbiddenTypes)
        {
            if (byType.ContainsKey(forbiddenType.Type) == false)
                byType.Add(forbiddenType.Type, forbiddenType);
        }

        return byType.Values
            .OrderBy(type => type.DeclarationOrder)
            .ThenBy(type => type.DisplayName, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private string? GetInvalidForbiddenTypeReason(ITypeSymbol type, RestrictionKind restrictionKind)
    {
        if (IsOpenGenericType(type))
            return $"'{type.ToDisplayString(TypeDisplayFormat)}' is an open or unbound generic type";

        if (ContainsTypeParameter(type))
            return $"'{type.ToDisplayString(TypeDisplayFormat)}' contains a generic parameter";

        if (restrictionKind == RestrictionKind.Assignable && type.SpecialType == SpecialType.System_Object)
            return "DisallowTypes cannot be configured with System.Object";

        return null;
    }

    private static bool IsOpenGenericType(ITypeSymbol type)
        => type switch
        {
            INamedTypeSymbol { IsUnboundGenericType: true } => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(IsOpenGenericType),
            IArrayTypeSymbol arrayType => IsOpenGenericType(arrayType.ElementType),
            IPointerTypeSymbol pointerType => IsOpenGenericType(pointerType.PointedAtType),
            _ => false
        };

    private static bool ContainsTypeParameter(ITypeSymbol type)
        => type switch
        {
            ITypeParameterSymbol => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsTypeParameter),
            IArrayTypeSymbol arrayType => ContainsTypeParameter(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsTypeParameter(pointerType.PointedAtType),
            _ => false
        };

    private static bool ContainsErrorType(ITypeSymbol type)
        => type switch
        {
            IErrorTypeSymbol => true,
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsErrorType),
            IArrayTypeSymbol arrayType => ContainsErrorType(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsErrorType(pointerType.PointedAtType),
            _ => false
        };

    private static Diagnostic CreateInvalidConfigurationDiagnostic(
        AttributeData attribute,
        ITypeParameterSymbol typeParameter,
        RestrictionKind restrictionKind,
        string reason,
        CancellationToken cancellationToken)
        => Diagnostic.Create(
            TypeSafetyDiagnosticDescriptors.InvalidConfiguration,
            GetAttributeLocation(attribute, typeParameter, cancellationToken),
            GetConfigurationName(restrictionKind),
            typeParameter.Name,
            reason);

    private static Diagnostic CreateMalformedAttributeDiagnostic(
        AttributeData attribute,
        ITypeParameterSymbol typeParameter,
        string reason,
        CancellationToken cancellationToken)
        => Diagnostic.Create(
            TypeSafetyDiagnosticDescriptors.MalformedAttributeMetadata,
            GetAttributeLocation(attribute, typeParameter, cancellationToken),
            attribute.AttributeClass?.ToDisplayString(TypeDisplayFormat) ?? "<unknown>",
            reason);

    private static string GetConfigurationName(RestrictionKind restrictionKind)
        => restrictionKind switch
        {
            RestrictionKind.Assignable => "DisallowTypes",
            RestrictionKind.Exact => "DisallowExactTypes",
            _ => "type-safety attribute"
        };

    private static Location GetAttributeLocation(AttributeData attribute, ITypeParameterSymbol typeParameter, CancellationToken cancellationToken)
    {
        var syntax = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
        if (syntax is not null)
            return syntax.GetLocation();

        return typeParameter.Locations.FirstOrDefault() ?? Location.None;
    }
}
