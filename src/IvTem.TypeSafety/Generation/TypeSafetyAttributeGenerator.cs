using Microsoft.CodeAnalysis;

namespace IvTem.TypeSafety.Generation;

[Generator(LanguageNames.CSharp)]
public sealed class TypeSafetyAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(static productionContext =>
        {
            productionContext.AddSource(EmbeddedAttributeSource.HintName, EmbeddedAttributeSource.Source);
        });
}
