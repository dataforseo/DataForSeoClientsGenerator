using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.CodeGen.CSharp.Models;

namespace OpenApiGenerator.CodeGen.CSharp;

public class CSharpNamespaceResolver : NamespaceResolver
{
    public sealed override CSharpGeneratorSettings Settings { get; }

    public CSharpNamespaceResolver(CSharpGeneratorSettings settings)
    {
        Settings = settings;
    }

    public override void ResolveNamespace(LiquidFileBinding binding)
    {
        var nmspc = Settings.RootNamespace;
        switch (binding)
        {
            case LiquidApiBinding or LiquidApiTestsBinding:
                nmspc += ".Api";
                break;
            case LiquidDtoBinding or LiquidDtoInheritanceBinding:

                if (binding.ClassName.EndsWith("RequestInfo"))
                {
                    nmspc += ".Models.Requests";
                    break;
                }
                
                if (binding.ClassName.EndsWith("ResponseInfo"))
                {
                    nmspc += ".Models.Responses";
                    break;
                }
                
                nmspc += ".Models";
                break;
        }

        binding.Namespace = nmspc;
    }

    public override string ResolveFilePath(LiquidFileBinding binding)
    {
        //TODO
        var root = Settings.RootFilePath;
        if (binding == null)
            return root;

        if (string.IsNullOrEmpty(binding.ClassName))
            throw new Exception("Can't define file path before define class name");

        var file = string.Empty;
        switch (binding)
        {
            case LiquidApiBinding:
            {
                root = Path.Combine(root, "src");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);

                file = $"{binding.ClassName}.{Settings.FileType}";

                binding.FilePath = Path.Combine(root, "Api", file);
                return binding.FilePath;
            }
            case LiquidDtoBinding:
            {
                root = Path.Combine(root, "src");
                if (string.IsNullOrEmpty(binding.Namespace))
                    ResolveNamespace(binding);

                file = $"{binding.ClassName}.{Settings.FileType}";

                var finalPath = Path.Combine(root, "Models");

                if (binding.ClassName.EndsWith("RequestInfo"))
                    finalPath = Path.Combine(finalPath, "Requests");
                
                if (binding.ClassName.EndsWith("ResponseInfo"))
                    finalPath = Path.Combine(finalPath, "Responses");
                
                binding.FilePath = Path.Combine(finalPath, file);
                return binding.FilePath;
            }
            case LiquidDocumentationBinding:
            {
                root = Path.Combine(root, "docs");
                file = $"{binding.ClassName}.md";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidApiTestsBinding:
            {
                root = Path.Combine(root, "tests");
                file = $"{binding.ClassName}.{Settings.FileType}";
                binding.FilePath = Path.Combine(root, "Api", file);
                return binding.FilePath;
            }
            case LiquidMainClientBinding:
            {
                root = Path.Combine(root, "src");
                file = $"{binding.ClassName}.{Settings.FileType}";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidResourceFileBinding resource:
            {
                root = Path.Combine(root, "src");
                resource.FileType ??= Settings.FileType;
                file = $"{resource.ClassName}.{resource.FileType}";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            default:
                return root;
        }
    }
}