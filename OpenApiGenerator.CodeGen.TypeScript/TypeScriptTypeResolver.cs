using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.TypeScript;

public class TypeScriptTypeResolver : TypeResolver
{
    public TypeScriptTypeResolver(CodeGeneratorSettingsBase settings) : base(settings)
    {
    }

    protected override ResolvedTypeInfo ResolveArray(OpenApiSchema schema)
    {
        var items = Resolve(schema.Items);

        var values = schema.Example is OpenApiArray array
            ? array.Select(x => ExtractPrimitive(x)).ToList()
            : [ExtractPrimitive(schema.Example)];

        values = values.Where(x => !string.IsNullOrEmpty(x)).ToList();

        return new ResolvedTypeInfo()
        {
            TypeName = $"{items.TypeName}[]",
            StructureType = "Array",
            Of = items,
            SourceType = items.SourceType,
            Value = values
                .Select(x => new ResolvedTypeValueInfo()
                {
                    Value = x,
                })
                .ToList()
        };
    }

    protected override ResolvedTypeInfo ResolveReference(OpenApiSchema schema)
    {
        var refId = schema.ReferenceId();
        return new ResolvedTypeInfo()
        {
            TypeName = refId,
            StructureType = "Object",
            SourceType = refId,
        };
    }

    protected override ResolvedTypeInfo ResolveDicionary(OpenApiSchema schema)
    {
        var dictValueInfo = Resolve(schema.AdditionalProperties);

        var values = new List<ResolvedTypeValueInfo>();
        if (schema.Example != null)
        {
            var example = schema.Example as OpenApiObject;
            foreach (var (name, value) in example.ToList())
            {
                values.Add(new ResolvedTypeValueInfo()
                {
                    Name = $"\"{name}\"",
                    Value = ExtractPrimitive(value),
                });
            }
        }

        return new ResolvedTypeInfo()
        {
            TypeName = $"{{ [key: string]: {dictValueInfo.TypeName}; }}",
            StructureType = "Dictionary",
            SourceType = dictValueInfo.SourceType,
            Of = dictValueInfo,
            Value = values
        };
    }

    protected override ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema)
    {
        return new ResolvedTypeInfo()
        {
            TypeName = "any",
            StructureType = "Any",
        };
    }

    protected override ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema)
    {
        var type = schema.Type switch
        {
            "string" => "string",
            "integer" or "number" => "number",
            "boolean" => "boolean",
            _ => "any"
        };
        
        string val = ExtractPrimitive(schema.Example);

        return new ResolvedTypeInfo()
        {
            TypeName = type,
            StructureType = "Primitive",
            Value = string.IsNullOrEmpty(val)
                ? null
                : new List<ResolvedTypeValueInfo>()
                {
                    new()
                    {
                        Value = val,
                    }
                }
        };
    }
    
    private string ExtractPrimitive(IOpenApiAny schema)
    {
        var res = schema switch
        {
            OpenApiString str => str.Value,
            OpenApiInteger integer => integer.Value.ToString(),
            OpenApiLong longValue => longValue.Value.ToString(),
            OpenApiFloat floatValue => floatValue.Value.ToString(),
            OpenApiDouble doubleValue => doubleValue.Value.ToString(),
            OpenApiBoolean boolValue => boolValue.Value.ToString().ToLower(),
            _ => null
        };

        if (string.IsNullOrEmpty(res))
            return null;

        res = res.Replace("\n", "\\n");
        if (schema is OpenApiString && !res.Contains("\""))
            res = $"\"{res}\"";

        return res;
    }
}