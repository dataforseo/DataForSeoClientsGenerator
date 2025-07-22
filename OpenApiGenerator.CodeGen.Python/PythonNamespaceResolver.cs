using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Python;

public class PythonNamespaceResolver : NamespaceResolver
{
    public override CodeGeneratorSettingsBase Settings { get; }

    public PythonNamespaceResolver(PythonGeneratorSettings settings)
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
                nmspc += ".models";
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
        
        var className = binding.ClassName.ToSnakeCase();
        var file = string.Empty;
        switch (binding)
        {
            case LiquidApiBinding or LiquidDtoBinding:
            {
                root = Path.Combine(root, "src");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);

                var nmspc = Path.Combine(binding.Namespace.Split('.'));
        
                file = $"{className}.{Settings.FileType}";
        
                binding.FilePath = Path.Combine(root, nmspc, file);
                return binding.FilePath;
            }
            case LiquidApiTestsBinding:
            {
                root = Path.Combine(root, "src", "tests");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);
        
                file = $"{className}.{Settings.FileType}";
                
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidDocumentationBinding:
            {
                root = Path.Combine(root, "docs");
                file = $"{className}.md";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidResourceFileBinding:
            {
                root = Path.Combine(root, binding.FilePath);
                file = $"{className}.{Settings.FileType}";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            // case LiquidPythonSetupBinding:
            // {
            //     root = Path.Combine(root, "src");
            //     file = $"{className}.py";
            //     binding.FilePath = Path.Combine(root, file);
            //     return binding.FilePath;
            // }
            default:
                return root;
        }
    }
}