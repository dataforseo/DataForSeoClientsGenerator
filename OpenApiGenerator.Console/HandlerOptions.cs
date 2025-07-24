using CodeGenerator.Core;

namespace OpenApiGenerator.Console;

public record HandlerOptions
{
    public List<LangaugeGenerateType> Langauges { get; set; }
    public string DocumentationPath { get; set; }
    public string SaveResultRootPath { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
}