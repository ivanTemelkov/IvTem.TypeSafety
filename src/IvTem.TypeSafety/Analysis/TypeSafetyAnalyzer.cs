using System.Collections.Immutable;
using System.Linq;
using IvTem.TypeSafety.Diagnostics;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
            var assignableTypeMatcher = new AssignableTypeMatcher(compilationStartContext.Compilation);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, extractor),
                SymbolKind.NamedType);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, extractor),
                SymbolKind.Method);

            compilationStartContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeGenericName(syntaxContext, extractor, exactTypeMatcher, assignableTypeMatcher),
                SyntaxKind.GenericName);

            compilationStartContext.RegisterOperationAction(
                operationContext => AnalyzeMethodUse(operationContext, extractor, exactTypeMatcher, assignableTypeMatcher),
                OperationKind.Invocation,
                OperationKind.DelegateCreation,
                OperationKind.Conversion);
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
        ExactTypeMatcher exactTypeMatcher,
        AssignableTypeMatcher assignableTypeMatcher)
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
            var matchedRestrictions = GetMatchedRestrictions(actualType, policy, exactTypeMatcher, assignableTypeMatcher);
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

    private static void AnalyzeMethodUse(
        OperationAnalysisContext context,
        DirectRestrictionPolicyExtractor extractor,
        ExactTypeMatcher exactTypeMatcher,
        AssignableTypeMatcher assignableTypeMatcher)
    {
        if (context.Operation is IConversionOperation && HasAncestorOperation<IConversionOperation>(context.Operation.Parent))
            return;

        if (context.Operation is IConversionOperation && HasAncestorOperation<IDelegateCreationOperation>(context.Operation.Parent))
            return;

        var method = GetConstructedMethod(context.Operation);
        if (method is null)
            return;

        if (method.IsGenericMethod == false)
            return;

        var typeArguments = method.TypeArguments;
        var typeParameters = method.OriginalDefinition.TypeParameters;
        if (typeArguments.Length != typeParameters.Length)
            return;

        for (var index = 0; index < typeArguments.Length; index++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var actualType = typeArguments[index];
            if (ContainsErrorType(actualType))
                continue;

            var policy = extractor.Extract(typeParameters[index], static _ => { }, context.CancellationToken);
            var matchedRestrictions = GetMatchedRestrictions(actualType, policy, exactTypeMatcher, assignableTypeMatcher);
            if (matchedRestrictions.Length == 0)
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                TypeSafetyDiagnosticDescriptors.ForbiddenGenericArgument,
                GetMethodTypeArgumentLocation(context.Operation.Syntax, index),
                actualType.ToDisplayString(TypeDisplayFormat),
                policy.TypeParameter.Name,
                method.OriginalDefinition.ToDisplayString(TypeDisplayFormat),
                FormatMatchedRestrictions(matchedRestrictions)));
        }
    }

    private static ImmutableArray<string> GetMatchedRestrictions(
        ITypeSymbol actualType,
        RestrictionPolicy policy,
        ExactTypeMatcher exactTypeMatcher,
        AssignableTypeMatcher assignableTypeMatcher)
        => policy.DisallowAssignable
            .Where(forbiddenType => assignableTypeMatcher.Matches(actualType, forbiddenType.Type))
            .Select(forbiddenType => FormatRestriction("DisallowTypes", forbiddenType))
            .Concat(policy.DisallowExact
                .Where(forbiddenType => exactTypeMatcher.Matches(actualType, forbiddenType.Type))
                .Select(forbiddenType => FormatRestriction("DisallowExactTypes", forbiddenType)))
            .ToImmutableArray();

    private static IMethodSymbol? GetConstructedMethod(IOperation operation)
        => operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IDelegateCreationOperation delegateCreation => GetConstructedMethod(delegateCreation.Target),
            IConversionOperation conversion => GetConstructedMethod(conversion.Operand),
            IMethodReferenceOperation methodReference => methodReference.Method,
            _ => null
        };

    private static Location GetMethodTypeArgumentLocation(SyntaxNode syntax, int typeArgumentIndex)
    {
        var genericName = GetMethodGenericName(syntax);
        if (genericName is not null && genericName.TypeArgumentList.Arguments.Count > typeArgumentIndex)
            return genericName.TypeArgumentList.Arguments[typeArgumentIndex].GetLocation();

        return syntax.GetLocation();
    }

    private static GenericNameSyntax? GetMethodGenericName(SyntaxNode syntax)
        => syntax switch
        {
            InvocationExpressionSyntax invocation => GetMethodGenericName(invocation.Expression),
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name as GenericNameSyntax,
            GenericNameSyntax genericName => genericName,
            _ => null
        };

    private static bool HasAncestorOperation<TOperation>(IOperation? operation)
        where TOperation : IOperation
    {
        for (var current = operation; current is not null; current = current.Parent)
        {
            if (current is TOperation)
                return true;
        }

        return false;
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
