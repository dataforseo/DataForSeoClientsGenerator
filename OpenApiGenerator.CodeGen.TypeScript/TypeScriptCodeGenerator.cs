using System.Text;
using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.CodeGen.TypeScript.Models;

namespace OpenApiGenerator.CodeGen.TypeScript;

public class TypeScriptCodeGenerator : BaseCodeGenerator
{
    protected sealed override TypeScriptGeneratorSettings Settings { get; }

    protected override List<string> SyntaxKeys { get; } = new()
    {
        "default"
    };
    private IResourceFactory _factory;
    
    public TypeScriptCodeGenerator(OpenApiDocument document, TypeScriptGeneratorSettings settings = null) : base(document)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _factory = Settings.EmbededResouceFactory;
    }

    public ICollection<CodeArtifact> GenerateCode()
    {
        var result = new List<CodeArtifact>();
        
        var mainIndexBinding = new TypeScriptIndexBinding("index");
        foreach (var group in CreateBindings().OfType<LiquidFileBinding>().GroupBy(x => x.Namespace))
        {
            var indexBinding = new TypeScriptIndexBinding("index");
            indexBinding.Types.AddRange(group.ToList());
            Settings.NamespaceResolver.ResolveNamespace(indexBinding);
            Settings.NamespaceResolver.ResolveFilePath(indexBinding);
            if (!string.IsNullOrEmpty(indexBinding.FilePath))
            {
                result.Add(new()
                {
                    Code = _factory.CreateTemplate(LiquidConfig.Create("Code.Index", indexBinding)).Render(),
                    FilePath = indexBinding.FilePath,
                });
            }

            foreach (var binding in group)
            {
                if (string.IsNullOrEmpty(binding.FilePath))
                    continue;

                if (binding is LiquidDtoBinding dto)
                {
                    if (!string.IsNullOrEmpty(dto.DiscriminatorValue)) //not create polymorph child item but add in index imports
                    {
                        indexBinding.Types.Add(binding);
                        mainIndexBinding.Types.Add(binding);
                        continue;
                    }

                    if (dto.Childs is { Count: > 0})
                    {
                        foreach (var child in dto.Childs)
                        {
                            dto.DependentTypes.AddRange(child.DependentTypes);
                            child.DependentTypes = [];
                        }

                        dto.DependentTypes = dto.DependentTypes.Where(x => x.ClassName != dto.ClassName).DistinctBy(x => x.ClassName).ToList();
                    }
                }
                
                mainIndexBinding.Types.Add(binding);
                
                var template = binding switch
                {
                    LiquidApiBinding => "Code.API",
                    LiquidDtoBinding => "Code.DTO",
                    LiquidDocumentationApiBinding => "Documentation.API",
                    LiquidDocumentationDtoBinding  => "Documentation.DTO",
                    LiquidApiTestsBinding => "Test.API",
                };
            
                result.Add(new()
                {
                    Code = _factory.CreateTemplate(LiquidConfig.Create(template, binding)).Render(),
                    FilePath = binding.FilePath,
                });
            }
        }
        mainIndexBinding.Types = mainIndexBinding.Types.Where(x => x is LiquidApiBinding or LiquidDtoBinding).ToList();
        Settings.NamespaceResolver.ResolveNamespace(mainIndexBinding);
        Settings.NamespaceResolver.ResolveFilePath(mainIndexBinding);
        result.Add(new()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create("Code.Index", mainIndexBinding)).Render(),
            FilePath = mainIndexBinding.FilePath,
        });
        result.AddRange(CreateResources());
        
        return result;
    }
    
    private List<CodeArtifact> CreateResources()
    {
        var binding = new LiquidResourceFileBinding("package")
        {
            FilePath = Path.Combine(Settings.RootFilePath, "package.json"),
            FileType = "json",
            Version = Settings.Version,
        };
        
        var package = new CodeArtifact()
        {
            Code = _factory.CreateTemplate(LiquidConfig.Create("Resource.PackageJson", binding)).Render(),
            FilePath = binding.FilePath,
        };

        return [package];
    }
}