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
        return new ResolvedTypeInfo()
        {
            TypeName = $"{items.TypeName}[]",
            StructureType = "Array",
            Of = items,
            SourceType = items.SourceType,
            Value = ResolveExample(schema.Example)
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
        return new ResolvedTypeInfo()
        {
            TypeName = $"{{ [key: string]: {dictValueInfo.TypeName}; }}",
            StructureType = "Dictionary",
            SourceType = dictValueInfo.SourceType,
            Of = dictValueInfo,
            Value = ResolveExample(schema.Example)
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