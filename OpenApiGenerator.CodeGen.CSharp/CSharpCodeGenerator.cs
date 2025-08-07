using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.CodeGen.CSharp.Models;

namespace OpenApiGenerator.CodeGen.CSharp;

public class CSharpCodeGenerator : BaseCodeGenerator
{
    protected sealed override CSharpGeneratorSettings Settings { get; }

    protected override List<string> SyntaxKeys { get; } = new()
    {
        "default"
    };

    private IResourceFactory _factory;
    
    public CSharpCodeGenerator(OpenApiDocument document, CSharpGeneratorSettings settings = null) : base(document)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _factory = Settings.EmbededResouceFactory;
    }
    
    public ICollection<CodeArtifact> GenerateCode()
    {
        var artifacts = new List<CodeArtifact>();
        var mainClientBinding = new LiquidMainClientBinding("DataForSeoClient");
        mainClientBinding.Version = Settings.Version;
        Settings.NamespaceResolver.ResolveNamespace(mainClientBinding);
        Settings.NamespaceResolver.ResolveFilePath(mainClientBinding);

        var item = CreateBindings();
        foreach (var bind in CreateBindings().OfType<LiquidFileBinding>())
        {
            var templateName = bind switch
            {
                LiquidApiBinding => "Code.API",
                LiquidDtoBinding => "Code.DTO",
                LiquidDocumentationApiBinding => "Documentation.API",
                LiquidDocumentationDtoBinding => "Documentation.DTO",
                LiquidApiTestsBinding => "Test.API",
                _ => null
            };

            if (string.IsNullOrEmpty(templateName))
                continue;

            if (templateName == "Code.API")
                mainClientBinding.ApiList.Add(bind.ClassName);
            
            artifacts.Add(new CodeArtifact()
            {
                Code = _factory.CreateTemplate(LiquidConfig.Create(templateName, bind)).Render(),
                FilePath = bind.FilePath
            });
        }
        
        
        artifacts.Add(new CodeArtifact()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create("Code.MainClient", mainClientBinding)).Render(),
            FilePath =  mainClientBinding.FilePath
        });

        var projectFileBinding = new LiquidResourceFileBinding("DataForSeo.Client")
        {
            Version = Settings.Version,
            FileType = "csproj",
        };
        Settings.NamespaceResolver.ResolveFilePath(projectFileBinding);
        
        artifacts.Add(new CodeArtifact()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create("ProjectFile", projectFileBinding)).Render(),
            FilePath =  projectFileBinding.FilePath
        });
        return artifacts;
    }
}