using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.Java;

public class JavaCodeGenerator : BaseCodeGenerator
{
    protected sealed override JavaGeneratorSettings Settings { get; }

    protected override List<string> SyntaxKeys { get; } = new()
    {
        "default"
    };

    private IResourceFactory _factory;

    public JavaCodeGenerator(OpenApiDocument document, JavaGeneratorSettings settings = null) : base(document)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _factory = Settings.EmbededResouceFactory;
    }

    public ICollection<CodeArtifact> GenerateCode()
    {
        var artifacts = GetDefaultArtifacts();
        var bindings = CreateBindings();
        artifacts.Add(BuildJsonClass(bindings));
        
        foreach (var bind in bindings.OfType<LiquidFileBinding>())
        {
            var templateName = bind switch
            {
                LiquidApiBinding => "Code.API",
                LiquidDtoBinding => "Code.DTO",
                LiquidDocumentationApiBinding => "Documentation.API",
                LiquidDocumentationDtoBinding => "Documentation.DTO",
                LiquidApiTestsBinding => "Test.API",
                _ => throw new ArgumentOutOfRangeException(nameof(bind))
            };
            
            artifacts.Add(new CodeArtifact()
            {
                Code = _factory.CreateTemplate(LiquidConfig.Create(templateName, bind)).Render(),
                FilePath = bind.FilePath
            });
        }

        return artifacts;
    }

    private CodeArtifact BuildJsonClass(List<LiquidBinding> classes)
    {
        var dtoModels = classes.OfType<LiquidDtoBinding>().ToList();
        var jsonBinding = new
        {
            ClassName = "JSON",
            Namespace = Settings.RootNamespace,
            Discriminators = dtoModels
                .Where(x => x.IsParent)
                .Select(x => new
                {
                    Namespace = x.Namespace,
                    BaseClass = x.ClassName,
                    DiscriminatorProperty = x.DiscriminatorProperty,
                    Childs = x.Childs
                        .Select(xx => new
                        {
                            Namespace = x.Namespace,
                            Value = xx.DiscriminatorValue,
                            ClassName = xx.ClassName,
                        })
                        .ToList()
                })
                .ToList(),
            DtoClasses = dtoModels
        };
        
        var result = new CodeArtifact
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create($"Code.JSON", jsonBinding)).Render(),
            FilePath = Settings.NamespaceResolver.ResolveFilePath(new LiquidResourceFileBinding("JSON")
            {
                Namespace = Settings.RootNamespace,
                FilePath = "src/main/java",
                FileType = "java",
            })
        };

        return result;
    }

    private ICollection<CodeArtifact> GetDefaultArtifacts()
    {
        var artifacts = new List<CodeArtifact>();

        LoadJavaTemplate("ApiCallback");
        LoadJavaTemplate("ApiClient");
        LoadJavaTemplate("ApiException");
        LoadJavaTemplate("ApiResponse");
        LoadJavaTemplate("Configuration");
        LoadJavaTemplate("GzipRequestInterceptor");
        LoadJavaTemplate("Pair");
        LoadJavaTemplate("ProgressRequestBody");
        LoadJavaTemplate("ProgressResponseBody");
        LoadJavaTemplate("ServerConfiguration");
        LoadJavaTemplate("ServerVariable");
        LoadJavaTemplate("StringUtil");

        LoadJavaTemplate("ApiKeyAuth", "auth");
        LoadJavaTemplate("Authentication", "auth");
        LoadJavaTemplate("HttpBasicAuth", "auth");
        LoadJavaTemplate("HttpBearerAuth", "auth");

        return artifacts;

        void LoadJavaTemplate(string name, string additionalNamespace = "")
        {
            var binding = new LiquidResourceFileBinding(name)
            {
                Namespace = string.IsNullOrEmpty(additionalNamespace)
                    ? Settings.RootNamespace
                    : $"{Settings.RootNamespace}.{additionalNamespace}",
                FilePath = "src/main/java",
                FileType = "java",
                Version = Settings.Version
            };
            artifacts.Add(new CodeArtifact()
            {
                Code = _factory.CreateTemplate(LiquidConfig.Create($"Code.{name}", binding)).Render(),
                FilePath = Settings.NamespaceResolver.ResolveFilePath(binding)
            });
        }
    }
}