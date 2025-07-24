using System.CommandLine;
using System.CommandLine.Parsing;
using CodeGenerator.Core;
using OpenApiGenerator.Console;

var cmdArgs = args;

#if DEBUG
Console.Write("Enter args: ");
var input = Console.ReadLine();

cmdArgs = CommandLineStringSplitter.Instance
    .Split(input ?? string.Empty)
    .ToArray();
#endif

var languagesParam = CliOptionsProvider.GetLangaugesOption();
var docParam = CliOptionsProvider.GetDocumantationPathOption();
var loginParam = CliOptionsProvider.GetLoginOption();
var passwordParam = CliOptionsProvider.GetPasswordOption();
var outputParam = CliOptionsProvider.GetOutputPathOption();
var cmd = new RootCommand("DFS client code generator cli")
{
    languagesParam,
    docParam,
    loginParam,
    passwordParam,
    outputParam
};

cmd.SetHandler(async (languages, docPath, login, password, outputPath) =>
{
    await GenerateHandler.Handle(new HandlerOptions()
    {
        Langauges = languages.ToList(),
        DocumentationPath = docPath,
        Login = login ?? "username",
        Password = password ?? "password",
        SaveResultRootPath = outputPath
    });
}, languagesParam, docParam, loginParam, passwordParam, outputParam);

await cmd.InvokeAsync(cmdArgs);