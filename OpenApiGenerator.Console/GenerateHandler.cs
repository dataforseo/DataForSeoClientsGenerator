using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using OpenApiGenerator.CodeGen.CSharp;
using OpenApiGenerator.CodeGen.Java;
using OpenApiGenerator.CodeGen.Python;
using OpenApiGenerator.CodeGen.TypeScript;

namespace OpenApiGenerator.Console;

public class GenerateHandler
{
    public static async Task Handle(HandlerOptions options)
    {
        var dfsYamlDocumentation = string.Empty;
        if (!string.IsNullOrEmpty(options.DocumentationPath))
        {
            dfsYamlDocumentation = await File.ReadAllTextAsync(options.DocumentationPath);
        }
        else
        {
            using var httpClient = new HttpClient();
            var githubUrlToDfsYamlFile = "https://raw.githubusercontent.com/dataforseo/OpenApiDocumentation/refs/heads/master/openapi_specification.yaml";
            dfsYamlDocumentation = await httpClient.GetStringAsync(githubUrlToDfsYamlFile);  
        }
      
        var documentation = new OpenApiStringReader().Read(dfsYamlDocumentation, out _);

        foreach (var langauge in options.Langauges)
        {
            switch (langauge)
            {
                case LangaugeGenerateType.CSharp:
                    await GenerateCSharpClient(options, documentation);
                    break;
                case LangaugeGenerateType.Java:
                    await GenerateJavaClient(options, documentation);
                    break;
                case LangaugeGenerateType.TypeScript:
                    await GenerateTypeScriptClient(options, documentation);
                    break;
                case LangaugeGenerateType.Python:
                    await GeneratePythonClient(options, documentation);
                    break;
                default:
                    throw new NotSupportedException($"Language {langauge} is not supported.");
            }
        }
    }

    private static async Task GeneratePythonClient(HandlerOptions options, OpenApiDocument documentation)
    {
        var pythonSettings = new PythonGeneratorSettings()
        {
            RootNamespace = "dataforseo_client",
            RootFilePath = Path.Combine(options.SaveResultRootPath, "Python"),
            Host = "api.dataforseo.com",
            FileType = "py",
            Version = "1.0.0",
            Sandbox =  new()
            {
                Host = "sandbox.dataforseo.com",
                Login = options.Login,
                Password = options.Password
            }
        };
        await new PythonClientProjectBuilder(pythonSettings).Build(documentation);
        System.Console.WriteLine($"Python client generated at '{pythonSettings.RootFilePath}'");
    }

    private static async Task GenerateTypeScriptClient(HandlerOptions options, OpenApiDocument documentation)
    {
        var tsSettings = new TypeScriptGeneratorSettings()
        {
            RootFilePath = Path.Combine(options.SaveResultRootPath, "TypeScript"),
            Host = "api.dataforseo.com",
            FileType = "ts",
            Version = "1.0.0",
            RootNamespace = "",
            Sandbox =  new()
            {
                Host = "sandbox.dataforseo.com",
                Login = options.Login,
                Password = options.Password
            }
        };
        await new TypeScriptClientProjectBuilder(tsSettings).Build(documentation);
        System.Console.WriteLine($"TypeScript client generated at '{tsSettings.RootFilePath}'");
    }

    private static async Task GenerateJavaClient(HandlerOptions options, OpenApiDocument documentation)
    {
        var javaSettings = new JavaGeneratorSettings()
        {
            RootNamespace = "io.github.dataforseo.client",
            RootFilePath = Path.Combine(options.SaveResultRootPath, "Java"),
            Host = "api.dataforseo.com",
            FileType = "java",
            Version = "1.0.0",
            Sandbox = new()
            {
                Host = "sandbox.dataforseo.com",
                Login = options.Login,
                Password = options.Password
            }
        };
        await new JavaClientProjectBuilder(javaSettings).Build(documentation);
        System.Console.WriteLine($"Java client generated at '{javaSettings.RootFilePath}'");
    }

    private static async Task GenerateCSharpClient(HandlerOptions options, OpenApiDocument documentation)
    {
        var csharpSettings = new CSharpGeneratorSettings()
        {
            RootNamespace = "DataForSeo.Client",
            RootFilePath = Path.Combine(options.SaveResultRootPath, "CSharp"),
            Host = "api.dataforseo.com",
            FileType = "cs",
            Version = "1.0.0",
            Sandbox =  new()
            {
                Host = "sandbox.dataforseo.com",
                Login = options.Login,
                Password = options.Password
            }
        };
        await new CSharpClientProjectBuilder(csharpSettings).Build(documentation);
        System.Console.WriteLine($"C# client generated at '{csharpSettings.RootFilePath}'");
    }
}