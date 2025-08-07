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
        return new ResolvedTypeInfo()
        {
            TypeName = $"List<{items.TypeName}>",
            StructureType = "Array",
            Of = items,
            Value = ResolveExample(schema.Example)
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
        return new ResolvedTypeInfo()
        {
            TypeName = $"Map<String, {dictValueInfo.TypeName}>",
            StructureType = "Dictionary",
            Of = dictValueInfo,
            Value = ResolveExample(schema.Example)
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

        return new ResolvedTypeInfo()
        {
            TypeName = type,
            StructureType = "Primitive",
            Value = ResolveExample(schema.Example)
        };
    }

    protected override string ExtractPrimitiveExample(IOpenApiAny schema)
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