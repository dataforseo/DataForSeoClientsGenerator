using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.Core;

public class OpenApiSchemaInfo
{
    public string Name { get; set; }
    public OpenApiSchema Schema { get; set; }
}