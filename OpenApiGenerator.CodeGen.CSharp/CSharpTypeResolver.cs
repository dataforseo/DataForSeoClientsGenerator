using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.CSharp;

public class CSharpTypeResolver : TypeResolver
{
    public CSharpTypeResolver(CodeGeneratorSettingsBase settings) : base(settings)
    {
    }

    protected override ResolvedTypeInfo ResolveArray(OpenApiSchema schema)
    {
        var items = Resolve(schema.Items);

        var values = schema.Example is OpenApiArray array
            ? array.Select(x => ExtractPrimitiveExample(x)).ToList()
            : [ExtractPrimitiveExample(schema.Example)];

        values = values.Where(x => !string.IsNullOrEmpty(x)).ToList();

        return new ResolvedTypeInfo()
        {
            TypeName = $"IEnumerable<{items.TypeName}>",
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
            SourceType = refId
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
                    Value = ExtractPrimitiveExample(value),
                });
            }
        }

        return new ResolvedTypeInfo()
        {
            TypeName = $"IDictionary<string, {dictValueInfo.TypeName}>",
            StructureType = "Dictionary",
            Of = dictValueInfo,
            Value = values,
            SourceType = dictValueInfo.SourceType
        };
    }

    protected override ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema)
    {
        return new ResolvedTypeInfo()
        {
            TypeName = "object",
            StructureType = "Any",
            SourceType = "object"
        };
    }

    protected override ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema)
    {
        var (type, sourceType) = schema.Type switch
        {
            "string" => ("string", "string"),
            "integer" => schema.Format switch
            {
                "int32" => ("int?", "int"),
                "int64" => ("long?", "long"),
                _ => ("int?", "int")
            },
            "number" => schema.Format switch
            {
                "float" => ("float?", "float"),
                "double" => ("double?", "double"),
                _ => ("double?", "double")
            },
            "boolean" => ("bool?", "bool"),
            _ => throw new NotImplementedException()
        };
        
        string val = ExtractPrimitiveExample(schema.Example);

        return new ResolvedTypeInfo()
        {
            TypeName = type,
            StructureType = "Primitive",
            SourceType = sourceType,
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
    
    private string ExtractPrimitiveExample(IOpenApiAny schema)
    {
        var res = schema switch
        {
            OpenApiString str => str.Value,
            OpenApiInteger integer => integer.Value.ToString(),
            OpenApiLong longValue => longValue.Value.ToString(),
            OpenApiFloat floatValue => floatValue.Value.ToString() + "f",
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