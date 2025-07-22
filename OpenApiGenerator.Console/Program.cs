using Microsoft.OpenApi.Readers;
using OpenApiGenerator.CodeGen.CSharp;
using OpenApiGenerator.CodeGen.Java;
using OpenApiGenerator.CodeGen.TypeScript;
using OpenApiGenerator.CodeGen.Python;

var path = "path to openapi yaml documentation";
var docStr = await File.ReadAllTextAsync(path);
var documentation = new OpenApiStringReader().Read(docStr, out _);

await new CSharpClientProjectBuilder(new CSharpGeneratorSettings()
{
    RootNamespace = "DataForSeo.Client",
    RootFilePath = "place to save generated files",
    Host = "api.dataforseo.com",
    FileType = "cs",
    Version = "1.0.0",
    Sandbox =
    {
        Host = "sandbox.dataforseo.com",
        Login = "username",
        Password = "password"
    }
}).Build(documentation);

await new JavaClientProjectBuilder(new JavaGeneratorSettings()
{
    RootNamespace = "io.github.dataforseo.client",
    RootFilePath = "place to save generated files",
    Host = "api.dataforseo.com",
    FileType = "java",
    Version = "1.0.0",
    Sandbox =
    {
        Host = "sandbox.dataforseo.com",
        Login = "username",
        Password = "password"
    }
}).Build(documentation);

await new TypeScriptClientProjectBuilder(new TypeScriptGeneratorSettings()
{
    RootFilePath = "place to save generated files",
    Host = "api.dataforseo.com",
    FileType = "ts",
    Version = "1.0.0",
    Sandbox =
    {
        Host = "sandbox.dataforseo.com",
        Login = "username",
        Password = "password"
    }
}).Build(documentation);

await new PythonClientProjectBuilder(new PythonGeneratorSettings()
{
    RootNamespace = "dataforseo_client",
    RootFilePath = "place to save generated files",
    Host = "api.dataforseo.com",
    FileType = "py",
    Version = "1.0.0",
    Sandbox =
    {
        Host = "sandbox.dataforseo.com",
        Login = "username",
        Password = "password"
    }
}).Build(documentation);