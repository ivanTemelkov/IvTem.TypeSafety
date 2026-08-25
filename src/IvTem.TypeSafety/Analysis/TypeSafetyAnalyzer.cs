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
            var constructedTypeUseValidator = new ConstructedTypeUseValidator(
                extractor,
                exactTypeMatcher,
                assignableTypeMatcher,
                new DiagnosticDeduplicator());

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, extractor),
                SymbolKind.NamedType);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, extractor),
                SymbolKind.Method);

            compilationStartContext.RegisterSyntaxNodeAction(
                syntaxContext => AnalyzeGenericName(syntaxContext, constructedTypeUseValidator),
                SyntaxKind.GenericName);

            compilationStartContext.RegisterOperationAction(
                operationContext => AnalyzeConstructedTypeOperation(operationContext, constructedTypeUseValidator),
                OperationKind.ObjectCreation,
                OperationKind.TypeOf,
                OperationKind.CollectionExpression);

            compilationStartContext.RegisterOperationAction(
                operationContext => AnalyzeMethodUse(operationContext, constructedTypeUseValidator),
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
        ConstructedTypeUseValidator constructedTypeUseValidator)
    {
        var genericName = (GenericNameSyntax)context.Node;
        if (IsAliasDeclaration(genericName))
            return;

        var type = context.SemanticModel.GetTypeInfo(genericName, context.CancellationToken).Type;
        if (type is not INamedTypeSymbol namedType)
            return;

        constructedTypeUseValidator.Validate(
            namedType,
            GetTypeArgumentLocations(genericName),
            genericName.GetLocation(),
            context.ReportDiagnostic,
            context.CancellationToken);
    }

    private static void AnalyzeMethodUse(
        OperationAnalysisContext context,
        ConstructedTypeUseValidator constructedTypeUseValidator)
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

        constructedTypeUseValidator.Validate(
            method,
            GetMethodTypeArgumentLocations(context.Operation.Syntax),
            context.Operation.Syntax.GetLocation(),
            context.ReportDiagnostic,
            context.CancellationToken);
    }

    private static void AnalyzeConstructedTypeOperation(
        OperationAnalysisContext context,
        ConstructedTypeUseValidator constructedTypeUseValidator)
    {
        var type = GetConstructedOperationType(context.Operation);
        if (type is null)
            return;

        constructedTypeUseValidator.Validate(
            type,
            GetTypeArgumentLocations(context.Operation.Syntax),
            context.Operation.Syntax.GetLocation(),
            context.ReportDiagnostic,
            context.CancellationToken);
    }

    private static IMethodSymbol? GetConstructedMethod(IOperation operation)
        => operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IDelegateCreationOperation delegateCreation => GetConstructedMethod(delegateCreation.Target),
            IConversionOperation conversion => GetConstructedMethod(conversion.Operand),
            IMethodReferenceOperation methodReference => methodReference.Method,
            _ => null
        };

    private static INamedTypeSymbol? GetConstructedOperationType(IOperation operation)
        => operation switch
        {
            IObjectCreationOperation objectCreation => objectCreation.Type as INamedTypeSymbol,
            ITypeOfOperation typeOf => typeOf.TypeOperand as INamedTypeSymbol,
            ICollectionExpressionOperation collectionExpression => collectionExpression.Type as INamedTypeSymbol,
            _ => null
        };

    private static ImmutableArray<Location> GetMethodTypeArgumentLocations(SyntaxNode syntax)
    {
        var genericName = GetMethodGenericName(syntax);
        if (genericName is not null)
            return GetTypeArgumentLocations(genericName);

        return ImmutableArray<Location>.Empty;
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

    private static ImmutableArray<Location> GetTypeArgumentLocations(SyntaxNode syntax)
    {
        var genericName = syntax switch
        {
            GenericNameSyntax directGenericName => directGenericName,
            ObjectCreationExpressionSyntax objectCreation => objectCreation.Type as GenericNameSyntax,
            TypeOfExpressionSyntax typeOf => typeOf.Type as GenericNameSyntax,
            _ => null
        };

        if (genericName is null)
            return ImmutableArray<Location>.Empty;

        return genericName.TypeArgumentList.Arguments
            .Select(argument => argument.GetLocation())
            .ToImmutableArray();
    }

    private static bool IsAliasDeclaration(GenericNameSyntax genericName)
        => genericName.Ancestors()
            .OfType<UsingDirectiveSyntax>()
            .Any(usingDirective => usingDirective.Alias is not null);

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
}
