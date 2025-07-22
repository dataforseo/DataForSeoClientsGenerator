using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.Java;

public class JavaNamespaceResolver : NamespaceResolver
{
    public sealed override JavaGeneratorSettings Settings { get; }
    
    public JavaNamespaceResolver(JavaGeneratorSettings settings)
    {
        Settings = settings;
    }
    
    public override void ResolveNamespace(LiquidFileBinding binding)
    {
        var nmspc = Settings.RootNamespace;
        switch (binding)
        {
            case LiquidApiBinding or LiquidApiTestsBinding:
                nmspc += ".api";
                break;
            case LiquidDtoBinding or LiquidDtoInheritanceBinding:
                nmspc += ".model";
                break;
            case LiquidDocumentationBinding:
                nmspc += ".docs";
                break;
        }

        binding.Namespace = nmspc;
    }

    public override string ResolveFilePath(LiquidFileBinding binding)
    {
        var root = Settings.RootFilePath;
        if (binding == null)
            return root;
        
        if (string.IsNullOrEmpty(binding.ClassName))
            throw new Exception("Can't define file path before define class name");
        
        var file = string.Empty;
        switch (binding)
        {
            case LiquidApiBinding or LiquidDtoBinding:
            {
                root = Path.Combine(root, "src/main/java");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);

                var nmspc = string.Join('/', binding.Namespace.Split('.'));
        
                file = $"{binding.ClassName}.{Settings.FileType}";
        
                binding.FilePath = Path.Combine(root, nmspc, file);
                return binding.FilePath;
            }
            case LiquidApiTestsBinding:
            {
                root = Path.Combine(root, "src/test/java");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);

                var nmspc = string.Join('/', binding.Namespace.Split('.'));
        
                file = $"{binding.ClassName}.{Settings.FileType}";
        
                binding.FilePath = Path.Combine(root, nmspc, file);
                return binding.FilePath;
            }
            case LiquidDocumentationBinding:
            {
                root = Path.Combine(root, "docs");
                file = $"{binding.ClassName}.md";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidResourceFileBinding resourceFileBinding:
            {
                root = Path.Combine(root, resourceFileBinding.FilePath);
                if (string.IsNullOrEmpty(resourceFileBinding.ClassName))
                    throw new Exception("Can't define file path before define class name");
        
                var nmspc = string.Join('/', binding.Namespace.Split('.'));
                
                file = $"{resourceFileBinding.ClassName}.{resourceFileBinding.FileType}";
        
                resourceFileBinding.FilePath = Path.Combine(root, nmspc, file);
                return resourceFileBinding.FilePath;
            }
            default:
                return root;
        }
    }
}