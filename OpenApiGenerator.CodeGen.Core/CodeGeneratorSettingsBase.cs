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
    public KnowledgeBaseConfiguration KnowledgeBase { get; set; }
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

public class KnowledgeBaseConfiguration
{
    /// <summary>
    /// API class name (e.g. "SerpApi") -> endpoints used as examples in its knowledge-base file, in the given order.
    /// An endpoint is a path as in the OpenAPI spec (e.g. "/v3/serp/google/organic/task_get/advanced/{id}")
    /// or an absolute URL (e.g. "https://api.dataforseo.com/v3/serp/google/organic/live/advanced").
    /// </summary>
    public Dictionary<string, List<string>> Endpoints { get; set; } = new();
}