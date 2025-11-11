using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;

namespace OpenApiGenerator.CodeGen.Core.Extensions;

public static class OpenApiSchemaExtensions
{
    public static string ToYaml(this OpenApiSchema schema)
    {
        using (var stringWriter = new StringWriter())
        {
            var writer = new OpenApiYamlWriter(stringWriter);
            schema.SerializeAsV3(writer);
            return stringWriter.ToString();
        }
    }
}