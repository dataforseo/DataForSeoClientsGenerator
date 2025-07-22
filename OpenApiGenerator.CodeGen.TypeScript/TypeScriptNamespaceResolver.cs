using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.CodeGen.TypeScript.Models;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.TypeScript;

public class TypeScriptNamespaceResolver : NamespaceResolver
{
    public override CodeGeneratorSettingsBase Settings { get; }

    public TypeScriptNamespaceResolver(TypeScriptGeneratorSettings settings)
    {
        Settings = settings;
    }

    public override void ResolveNamespace(LiquidFileBinding binding)
    {
        var nmspc = Settings.RootNamespace;
        switch (binding)
        {
            case LiquidApiBinding:
                nmspc += ".api";
                break;
            case LiquidDtoBinding or LiquidDtoInheritanceBinding:
                nmspc += ".models";
                break;
            case LiquidDocumentationBinding:
                nmspc += ".docs";
                break;
            case TypeScriptIndexBinding indexBinding:
                if (indexBinding.Types.All(x => x is LiquidApiBinding))
                {
                    nmspc = $"{nmspc}.api";
                }
                if (indexBinding.Types.All(x => x is LiquidDtoBinding))
                {
                    nmspc = $"{nmspc}.models";
                }
                break;
        }

        nmspc = nmspc.Trim('.');
        binding.Namespace = nmspc;
    }

    public override string ResolveFilePath(LiquidFileBinding binding)
    {
        var root = Settings.RootFilePath;
        if (binding == null)
            return root;
        
        if (string.IsNullOrEmpty(binding.Namespace))
            ResolveNamespace(binding);
        
        if (string.IsNullOrEmpty(binding.ClassName))
            throw new Exception("Can't define file path before define class name");

        switch (binding)
        {
            case LiquidApiBinding or LiquidDtoBinding:
            {
                root = Path.Combine(root, "src");
                var nmspc = Path.Combine(binding.Namespace.Split('.'));
                var file = $"{binding.ClassName}.ts";
                binding.FilePath = Path.Combine(root, nmspc, file);
                return binding.FilePath;
            }
            case LiquidApiTestsBinding:
            {
                root = Path.Combine(root, "tests");
                var file = $"{binding.ClassName}.test.ts";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case LiquidDocumentationBinding:
            {
                root = Path.Combine(root, "docs");
                var file = $"{binding.ClassName}.md";
                binding.FilePath = Path.Combine(root, file);
                return binding.FilePath;
            }
            case TypeScriptIndexBinding indexFile:
            {
                var typesFilePathList = indexFile.Types
                    .Select(x => Path.GetDirectoryName(x.FilePath))
                    .Distinct()
                    .ToList();

                var typesFileDir = typesFilePathList.Count == 1
                    ? typesFilePathList.First()
                    : ExtractCommonPartOfPath(typesFilePathList);
                
                if (!indexFile.Types.All(x =>  x is LiquidApiBinding or LiquidDtoBinding))
                    return null;
                ;
                binding.FilePath = Path.Combine(typesFileDir, "index.ts");
                return binding.FilePath;
            }
            default:
            {
                binding.FilePath = root;
                return binding.FilePath;
            }
        }
    }

    private string ExtractCommonPartOfPath(List<string> typesFilePathList)
    {
        if (typesFilePathList.Count == 0)
            return string.Empty;

        var commonParts = typesFilePathList
            .Select(x => x.Split(Path.DirectorySeparatorChar))
            .ToList();

        var minLength = commonParts.Min(x => x.Length);
        var commonPart = new List<string>();

        for (var i = 0; i < minLength; i++)
        {
            var part = commonParts[0][i];
            if (commonParts.All(x => x[i] == part))
            {
                commonPart.Add(part);
            }
            else
            {
                break;
            }
        }

        return string.Join(Path.DirectorySeparatorChar.ToString(), commonPart);
    }
}