using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.Python;

public class PythonStaticFileSaveOptiona
{
    public List<string> SavePathes { get; set; }
    public string FileName { get; set; }
}

public class PythonClientProjectBuilder
{
    public PythonGeneratorSettings Settings { get; }

    public Dictionary<string, PythonStaticFileSaveOptiona> StaticFiles { get; }

    public PythonClientProjectBuilder(PythonGeneratorSettings settings = null)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        var root = Path.GetFullPath(Settings.RootFilePath, AppContext.BaseDirectory);
        StaticFiles = new Dictionary<string, PythonStaticFileSaveOptiona>()
        {
            ["__init__.py"] = new()
            {
                SavePathes =
                [
                    Path.Combine(root, "src/dataforseo_client"),
                    Path.Combine(root, "src/dataforseo_client/api"),
                    Path.Combine(root, "src/dataforseo_client/models"),
                ],
            },
            ["empty__init__.py"] = new()
            {
                FileName = "__init__.py",
                SavePathes = [Path.Combine(root, "src/tests")],
            },
            ["api_response.py"] = new()
            {
                SavePathes = [Path.Combine(root, "src/dataforseo_client")]
            },
            ["configuration.py"] = new()
            {
                SavePathes = [Path.Combine(root, "src/dataforseo_client")]
            },
            ["exceptions.py"] = new()
            {
                SavePathes = [Path.Combine(root, "src/dataforseo_client")]
            },
            ["rest.py"] = new()
            {
                SavePathes = [Path.Combine(root, "src/dataforseo_client")]
            },
            ["pytest.ini"] = new()
            {
                SavePathes = [Path.Combine(root, "src")]
            },
            ["README.md"] = new()
            {
                SavePathes = [Path.Combine(root, "src")]
            },
            ["vscode_launch.json"] = new()
            {
                FileName = "launch.json",
                SavePathes = [Path.Combine(root, "src/.vscode")]
            },
            ["vscode_settings.json"] = new()
            {
                FileName = "settings.json",
                SavePathes = [Path.Combine(root, "src/.vscode")]
            },
        };
    }

    public async Task Build(OpenApiDocument document)
    {
        var codeGen = new PythonCodeGenerator(document, Settings);
        foreach (var codeFile in codeGen.GenerateCode())
        {
            var path = Path.GetFullPath(codeFile.FilePath, AppContext.BaseDirectory);
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Delete(path);
            await File.WriteAllTextAsync(path, codeFile.Code);
        }

        foreach (var (fileName, options) in StaticFiles)
        {
            var resource = Settings.EmbededResouceFactory.ReadRawEmbeddedResource("StaticFiles." + fileName);

            foreach (var savePath in options.SavePathes)
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                var path = Path.Combine(savePath, options.FileName ?? fileName);
                await File.WriteAllTextAsync(path, resource);
            }
        }
    }
}