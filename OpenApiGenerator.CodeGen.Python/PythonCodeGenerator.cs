using System.Text;
using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.Python;

public class PythonCodeGenerator : BaseCodeGenerator
{
    protected sealed override PythonGeneratorSettings Settings { get; }

    protected override List<string> SyntaxKeys { get; } = new()
    {
        "yield"
    };
    private IResourceFactory _factory;
    
    public PythonCodeGenerator(OpenApiDocument document, PythonGeneratorSettings settings = null) : base(document)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _factory = Settings.EmbededResouceFactory;
    }

    public ICollection<CodeArtifact> GenerateCode()
    {
        var result = GetDefaultArtifacts();
        
        foreach (var binding in CreateBindings().OfType<LiquidFileBinding>())
        {
            var template = binding switch
            {
                LiquidApiBinding => "Code.API",
                LiquidDtoBinding => "Code.DTO",
                LiquidDocumentationApiBinding => "Documentation.API",
                LiquidDocumentationDtoBinding => "Documentation.DTO",
                LiquidApiTestsBinding => "Test.API",
            };
            
            result.Add(new()
            {
                Code = _factory.CreateTemplate(LiquidConfig.Create(template, binding)).Render(),
                FilePath = binding.FilePath,
            });
        }
        
        return result;
    }

    private ICollection<CodeArtifact> GetDefaultArtifacts()
    {
        var artifacts = new List<CodeArtifact>();
        
        var setupBinding = new LiquidResourceFileBinding("setup")
        {
            Version = Settings.Version,
            FilePath = "src"
        };
        artifacts.Add(new CodeArtifact()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create($"Code.Setup", setupBinding)).Render(),
            FilePath = Settings.NamespaceResolver.ResolveFilePath(setupBinding)
        });
        
        var apiClientBinding = new LiquidResourceFileBinding("api_client")
        {
            Version = Settings.Version,
            FilePath = "src/dataforseo_client"
        };
        artifacts.Add(new CodeArtifact()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create($"Code.ApiClient", apiClientBinding)).Render(),
            FilePath = Settings.NamespaceResolver.ResolveFilePath(apiClientBinding)
        });
        
        return artifacts;
    }
}