using System.Reflection;
using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;

namespace OpenApiGenerator.CodeGen.Python;

public class PythonGeneratorSettings : CodeGeneratorSettingsBase
{
    public override LangaugeGenerateType Language { get; } = LangaugeGenerateType.Python;

    public PythonGeneratorSettings()
    {
        EmbededResouceFactory = new DefaultEmbededResouceFactory(this, [GetType().GetTypeInfo().Assembly]);
        TypeResolver = new PythonTypeResolver(this);
        PropertyNameResolver = new SnakeCaseNameResolver();
        ApiMethodNameResolver = new SnakeCaseNameResolver();
        ClassNameResolver = new PascalCaseNameResolver();
        NamespaceResolver = new PythonNamespaceResolver(this);
    }

    public static PythonGeneratorSettings CreateDefault()
    {
        return new PythonGeneratorSettings();
    }
}