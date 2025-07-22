using OpenApiGenerator.CodeGen.Core;

namespace CodeGenerator.Core;


public abstract class CodeGeneratorSettingsBase
{    
    public string Version { get; set; }
    public abstract LangaugeGenerateType Language { get; }
    public string Host { get; set; }
    public IResourceFactory EmbededResouceFactory { get; set; }
    public TypeResolver TypeResolver { get; set; }
    public NameResolver PropertyNameResolver { get; set; }
    public NameResolver ApiMethodNameResolver { get; set; }
    public NameResolver ClassNameResolver { get; set; }
    public NamespaceResolver NamespaceResolver { get; set; }
    public SandboxConfiguration Sandbox { get; set; }
    public string TemplateDirectory { get; set; }
    public string RootNamespace { get; set; }
    public string FileType { get; set; }
    public string RootFilePath { get; set; }
}

public enum LangaugeGenerateType
{
    CSharp,
    Java,
    Python,
    TypeScript,
}

public class SandboxConfiguration
{
    public string Host { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string UserAgent { get; set; }
}