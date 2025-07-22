using System.Reflection;
using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;

namespace OpenApiGenerator.CodeGen.CSharp;

public class CSharpGeneratorSettings : CodeGeneratorSettingsBase
{
    public override LangaugeGenerateType Language { get; } = LangaugeGenerateType.CSharp;
    
    public CSharpGeneratorSettings()
    {
        EmbededResouceFactory = new DefaultEmbededResouceFactory(this, [GetType().GetTypeInfo().Assembly]);
        TypeResolver = new CSharpTypeResolver(this);
        PropertyNameResolver = new PascalCaseNameResolver();
        ApiMethodNameResolver = new PascalCaseNameResolver();
        NamespaceResolver = new CSharpNamespaceResolver(this);
        ClassNameResolver = new PascalCaseNameResolver();
    }

    public static CSharpGeneratorSettings CreateDefault()
    {
        return new CSharpGeneratorSettings();
    }
}