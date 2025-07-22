using Microsoft.OpenApi.Models;

namespace OpenApiGenerator.CodeGen.Core;

public class ValueGenerator
{
    public virtual string Generate(string name, OpenApiSchema property)
    {
        var example = property.Example?.ToString();
        if (!string.IsNullOrEmpty(example))
            return example;

        if ((!string.IsNullOrEmpty(name) && name.Contains("datetime")) || (!string.IsNullOrEmpty(property.Description) && property.Description.Contains("datetime")))
            return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

        try
        {

            return property.Type switch
            {
                "string" => "\"some key\"",
                "integer" => "1",
                "number" => "1",
                "boolean" => "true",
                "array" => Generate(name, property.Items)
            };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}