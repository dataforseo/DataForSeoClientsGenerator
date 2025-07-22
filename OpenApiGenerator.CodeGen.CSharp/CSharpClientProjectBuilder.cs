using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.CSharp;

public class CSharpClientProjectBuilder
{
    public CSharpGeneratorSettings Settings { get; }

    public Dictionary<string, string> StaticFiles { get; }

    public CSharpClientProjectBuilder(CSharpGeneratorSettings settings = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        var root = Path.GetFullPath(Settings.RootFilePath, AppContext.BaseDirectory);
        StaticFiles = new Dictionary<string, string>()
        {
            ["ApiException.cs"] = Path.Combine(root, "src", "Models"),
            ["JsonInheritanceAttribute.cs"] = Path.Combine(root, "src", "Models"),
            ["JsonInheritanceConverter.cs"] = Path.Combine(root, "src", "Models"),
            ["README.md"] = Path.Combine(root, "src"),
            ["ClientTestsProject.csproj"] = Path.Combine(root, "tests"),
            ["TestHelper.cs"] = Path.Combine(root, "tests"),
        };
    }

    public async Task Build(OpenApiDocument document)
    {
        var codeGen = new CSharpCodeGenerator(document, Settings);
        foreach (var codeFile in codeGen.GenerateCode())
        {
            var path = Path.GetFullPath(codeFile.FilePath, AppContext.BaseDirectory);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, codeFile.Code);
        }
        
        foreach (var (fileName, savePath) in StaticFiles)
        {
            var resource = Settings.EmbededResouceFactory.ReadRawEmbeddedResource("StaticFiles." + fileName);

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            
            await File.WriteAllTextAsync(Path.Combine(savePath, fileName), resource);
        }
    }
}