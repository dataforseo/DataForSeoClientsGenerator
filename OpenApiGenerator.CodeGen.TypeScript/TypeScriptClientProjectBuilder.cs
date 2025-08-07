using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.TypeScript;

public class TypeScriptClientProjectBuilder
{
    public TypeScriptGeneratorSettings Settings { get; }

    public Dictionary<string, string> StaticFiles { get; }

    public TypeScriptClientProjectBuilder(TypeScriptGeneratorSettings settings = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        var root = Path.GetFullPath(Settings.RootFilePath, AppContext.BaseDirectory);
        StaticFiles = new Dictionary<string, string>()
        {
            ["ApiException.ts"] = Path.Combine(root, "src", "models"),
            ["tsconfig.base.json"] = root,
            ["tsconfig.cjs.json"] = root,
            ["tsconfig.esm.json"] = root,
            ["fix-esm-imports.js"] = root,
            // ["rollup.config.cjs.js"] = root,
            // ["rollup.config.esm.js"] = root,
            ["jest.config.cjs"] = root,
            ["README.md"] = root,
        };
    }

    public async Task Build(OpenApiDocument document)
    {
        var codeGen = new TypeScriptCodeGenerator(document, Settings);
        foreach (var codeFile in codeGen.GenerateCode())
        {
            var path = Path.GetFullPath(codeFile.FilePath, AppContext.BaseDirectory);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Delete(path);
            await File.WriteAllTextAsync(path, codeFile.Code);
        }

        foreach (var (fileName, savePath) in StaticFiles)
        {
            var resource = Settings.EmbededResouceFactory.ReadRawEmbeddedResource("StaticFiles." + fileName);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            var path = Path.Combine(savePath, fileName);
            await File.WriteAllTextAsync(path, resource);
        }
    }
}