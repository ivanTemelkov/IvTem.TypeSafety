using System.Collections.Immutable;
using IvTem.TypeSafety.Diagnostics;
using IvTem.TypeSafety.Policies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

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

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeNamedType(symbolContext, extractor),
                SymbolKind.NamedType);

            compilationStartContext.RegisterSymbolAction(
                symbolContext => AnalyzeMethod(symbolContext, extractor),
                SymbolKind.Method);
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
}
