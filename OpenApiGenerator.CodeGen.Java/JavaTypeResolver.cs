using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Java;

public class JavaTypeResolver : TypeResolver
{
    public JavaTypeResolver(CodeGeneratorSettingsBase settings) : base(settings)
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
            TypeName = $"List<{items.TypeName}>",
            StructureType = "Array",
            Of = items,
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
            StructureType = "Object"
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
            TypeName = $"Map<String, {dictValueInfo.TypeName}>",
            StructureType = "Dictionary",
            Of = dictValueInfo,
            Value = values
        };
    }

    protected override ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema)
    {
        return new ResolvedTypeInfo()
        {
            TypeName = "Object",
            StructureType = "Any",
        };
    }

    protected override ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema)
    {
        var type = schema.Type switch
        {
            "string" => "String",
            "integer" => schema.Format switch
            {
                "int32" => "Integer",
                "int64" => "Long",
                _ => "Integer"
            },
            "number" => schema.Format switch
            {
                "float" => "Float",
                "double" => "Double",
                _ => "Double"
            },
            "boolean" => "Boolean",
            _ => "Object"
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
            OpenApiLong longValue => longValue.Value.ToString() + "l",
            OpenApiFloat floatValue => floatValue.Value.ToString() + "f",
            OpenApiDouble doubleValue => doubleValue.Value.ToString() + "d",
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