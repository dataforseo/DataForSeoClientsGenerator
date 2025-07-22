using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.Utils.Extensions;

public static class OpenApiSchemaExtensions
{
    public static string ReferenceId(this OpenApiSchema schema)
    {
        var item = schema?.Items?.AllOf?.LastOrDefault()?.ReferenceId()
                   ?? schema?.Items?.OneOf?.FirstOrDefault()?.ReferenceId()
                   ?? schema?.Items?.ReferenceId()
                   ?? schema?.AllOf?.LastOrDefault()?.ReferenceId()
                   ?? schema?.OneOf?.FirstOrDefault()?.ReferenceId()
                   ?? schema?.Reference?.Id;

        return item;
    }
    
    public static bool IsAnyObjectSchema(this OpenApiSchema schema)
    {
        return schema.Type is "object" && schema.AdditionalProperties == null && schema.Properties.Count == 0 && string.IsNullOrEmpty(schema.ReferenceId());
    }
    
    public static string ReferenceName(this OpenApiSchema schema)
    {
        var referenceId = schema?.Items?.AllOf?.FirstOrDefault()?.ReferenceId()
                          ?? schema?.Items?.OneOf?.FirstOrDefault()?.ReferenceId()
                          ?? schema?.Items?.ReferenceId()
                          ?? schema?.AllOf?.FirstOrDefault()?.ReferenceId()
                          ?? schema?.OneOf?.FirstOrDefault()?.ReferenceId()
                          ?? schema?.Reference?.Id;

        if (!string.IsNullOrEmpty(referenceId))
            return referenceId.Split('/').Last();

        return null;
    }
}