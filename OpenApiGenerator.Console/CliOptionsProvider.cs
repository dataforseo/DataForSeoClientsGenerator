using System.CommandLine;
using CodeGenerator.Core;

namespace OpenApiGenerator.Console;

public class CliOptionsProvider
{
    public static Option<List<LangaugeGenerateType>> GetLangaugesOption()
    {
        var name = "--languages";
        var description = $"The programming languages to generate code for, by default generate for all available langauges. " +
                          $"Comma-separated list of languages (e.g. --languages Java,Python). " +
                          $"Available options: {string.Join(", ", Enum.GetNames(typeof(LangaugeGenerateType)))}";
        var languagesOption = new Option<List<LangaugeGenerateType>>(name, description)
        {
            AllowMultipleArgumentsPerToken = true,
        };
        languagesOption.SetDefaultValue(Enum.GetValues<LangaugeGenerateType>().ToList());
         languagesOption.AddValidator(optionResult =>
         {
             foreach (var token in optionResult.Tokens)
             {
                 var valueString = token.Value?.Trim();
                 try
                 {
                    Enum.Parse<LangaugeGenerateType>(valueString);
                 }
                 catch (Exception e)
                 {
                        optionResult.ErrorMessage = $"Invalid language '{valueString}' at option '{name}'. Available options: {string.Join(", ", Enum.GetNames(typeof(LangaugeGenerateType)))}";
                        return;
                 }
             }
             var test = 0;
         });
        return languagesOption;
    }

    public static Option<string> GetDocumantationPathOption()
    {
        var name = "--doc";
        var description = "The path to the OpenAPI YAML file. By default will be loaded from official GitHub repo: https://raw.githubusercontent.com/dataforseo/OpenApiDocumentation/refs/heads/master/openapi_specification.yaml";
        var docParam = new Option<string>(name, description);
        docParam.AddValidator(result =>
        {
            var value = result.GetValueForOption(docParam);
            if (!string.IsNullOrEmpty(value) && !Path.IsPathRooted(value))
            {
                result.ErrorMessage = $"The provided path in option '{name}' is not valid. Please provide an absolute path.";
            }
        });
        return docParam;
    }

    public static Option<string> GetLoginOption()
    {
        var loginParam = new Option<string>("--login", "The username for DataForSEO API. Need to generate api test classes with correct auth configuration. By default passes 'username' as login.");
        return loginParam;
    }

    public static Option<string> GetPasswordOption()
    {
        var passwordParam = new Option<string>("--password", "The username for DataForSEO API. Need to generate api test classes with correct auth configuration. By default passes 'password' as password.");
        return passwordParam;
    }

    public static Option<string> GetOutputPathOption()
    {
        var name = "--out";
        var description = "The output root path for generated code. By default will be used solution directory.";
        var outputParam = new Option<string>(name, description);
        outputParam.AddValidator(result =>
        {
            var value = result.GetValueForOption(outputParam);
            if (!string.IsNullOrEmpty(value) && !Path.IsPathRooted(value))
            {
                result.ErrorMessage = $"The provided path in option '{name}' is not valid. Please provide an absolute path.";
            }
        });
        outputParam.SetDefaultValue(Environment.CurrentDirectory);
        return outputParam;
    }
}