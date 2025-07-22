using System.Reflection;
using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;

namespace OpenApiGenerator.CodeGen.TypeScript;

public class TypeScriptGeneratorSettings : CodeGeneratorSettingsBase
{
    public override LangaugeGenerateType Language { get; } = LangaugeGenerateType.TypeScript;
    
    public TypeScriptGeneratorSettings()
    {
        EmbededResouceFactory = new DefaultEmbededResouceFactory(this, [GetType().GetTypeInfo().Assembly]);
        TypeResolver = new TypeScriptTypeResolver(this);
        PropertyNameResolver = new SnakeCaseNameResolver();
        ApiMethodNameResolver = new CamelCaseNameResolver();
        ClassNameResolver = new PascalCaseNameResolver();
        NamespaceResolver = new TypeScriptNamespaceResolver(this);
    }
}