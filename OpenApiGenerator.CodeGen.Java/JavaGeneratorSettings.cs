using System.Reflection;
using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;

namespace OpenApiGenerator.CodeGen.Java;

public class JavaGeneratorSettings : CodeGeneratorSettingsBase
{
    public override LangaugeGenerateType Language { get; } = LangaugeGenerateType.Java;
    public JavaGeneratorSettings()
    {
        EmbededResouceFactory = new DefaultEmbededResouceFactory(this, [GetType().GetTypeInfo().Assembly]);
        TypeResolver = new JavaTypeResolver(this);
        PropertyNameResolver = new CamelCaseNameResolver();
        ApiMethodNameResolver = new CamelCaseNameResolver();
        ClassNameResolver = new PascalCaseNameResolver();
        NamespaceResolver = new JavaNamespaceResolver(this);
    }

    public static JavaGeneratorSettings CreateDefault()
    {
        return new JavaGeneratorSettings();
    }
}