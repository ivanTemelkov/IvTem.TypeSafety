using System.Collections.Immutable;
using System.Linq;
using IvTem.TypeSafety.Diagnostics;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace IvTem.TypeSafety.Analysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeSafetyAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => TypeSafetyDiagnosticDescriptors.All;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationStartContext =>
        {
            var extractor = new DirectRestrictionPolicyExtractor();
            var exactTypeMatcher = new ExactTypeMatcher(compilationStartContext.Compilation);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, extractor),
                SymbolKind.NamedType);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, extractor),
                SymbolKind.Method);

            compilationStartContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeGenericName(syntaxContext, extractor, exactTypeMatcher),
                SyntaxKind.GenericName);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context, DirectRestrictionPolicyExtractor extractor)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        foreach (var typeParameter in namedType.TypeParameters)
            _ = extractor.Extract(typeParameter, context.ReportDiagnostic, context.CancellationToken);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, DirectRestrictionPolicyExtractor extractor)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise)
            return;

        foreach (var typeParameter in method.TypeParameters)
            _ = extractor.Extract(typeParameter, context.ReportDiagnostic, context.CancellationToken);
    }

    private static void AnalyzeGenericName(
        SyntaxNodeAnalysisContext context,
        DirectRestrictionPolicyExtractor extractor,
        ExactTypeMatcher exactTypeMatcher)
    {
        var genericName = (GenericNameSyntax)context.Node;
        var type = context.SemanticModel.GetTypeInfo(genericName, context.CancellationToken).Type;
        if (type is not INamedTypeSymbol namedType)
            return;

        if (namedType.IsUnboundGenericType)
            return;

        var typeArguments = namedType.TypeArguments;
        var typeParameters = namedType.OriginalDefinition.TypeParameters;
        if (typeArguments.Length != typeParameters.Length)
            return;

        for (var index = 0; index < typeArguments.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var actualType = typeArguments[index];
            if (ContainsErrorType(actualType))
                continue;

            var policy = extractor.Extract(typeParameters[index], static _ => { }, context.CancellationToken);
            var matchedRestrictions = policy.DisallowExact
                .Where(forbiddenType => exactTypeMatcher.Matches(actualType, forbiddenType.Type))
                .ToImmutableArray();

            if (matchedRestrictions.Length == 0)
                continue;

            var location = genericName.TypeArgumentList.Arguments[index].GetLocation();
            context.ReportDiagnostic(Diagnostic.Create(
                TypeSafetyDiagnosticDescriptors.ForbiddenGenericArgument,
                location,
                actualType.ToDisplayString(TypeDisplayFormat),
                policy.TypeParameter.Name,
                namedType.OriginalDefinition.ToDisplayString(TypeDisplayFormat),
                FormatMatchedRestrictions(matchedRestrictions)));
        }
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

    private static string FormatMatchedRestrictions(ImmutableArray<ForbiddenType> matchedRestrictions)
        => string.Join(
            ", ",
            matchedRestrictions.Select(forbiddenType => "DisallowExactTypes(" + forbiddenType.DisplayName + ")"));
}
