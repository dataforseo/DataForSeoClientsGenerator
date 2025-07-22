using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.Java;

public class JavaClientProjectBuilder
{
    public JavaGeneratorSettings Settings { get; }

    public Dictionary<string, string> StaticFiles { get; }

    public JavaClientProjectBuilder(JavaGeneratorSettings settings = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        var root = Path.GetFullPath(Settings.RootFilePath, AppContext.BaseDirectory);
        StaticFiles = new Dictionary<string, string>()
        {
            ["pom.xml"] = root,
            ["junit-platform.properties"] = Path.Combine(root, "src", "test", "resources"),
            ["README.md"] = root,
        };
    }

    public async Task Build(OpenApiDocument document)
    {
        var codeGen = new JavaCodeGenerator(document, Settings);
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
            
            await File.WriteAllTextAsync(Path.Combine(savePath, fileName), resource);
        }
    }
}