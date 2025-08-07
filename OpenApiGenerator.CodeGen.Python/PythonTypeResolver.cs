using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Python;

public class PythonTypeResolver : TypeResolver
{
    public PythonTypeResolver(CodeGeneratorSettingsBase settings) : base(settings)
    {
    }

    protected override ResolvedTypeInfo ResolveArray(OpenApiSchema schema)
    {
        var items = Resolve(schema.Items);
        return new ResolvedTypeInfo()
        {
            TypeName = $"List[Optional[{items.TypeName}]]",
            StructureType = "Array",
            SourceType = items.SourceType,
            Of = items,
            Value = ResolveExample(schema.Example),
        };
    }

    protected override ResolvedTypeInfo ResolveReference(OpenApiSchema schema)
    {
        var refId = schema.ReferenceId();
        return new ResolvedTypeInfo()
        {
            TypeName = $"{refId}",
            SourceType = refId,
            StructureType = "Object"
        };
    }

    protected override ResolvedTypeInfo ResolveDicionary(OpenApiSchema schema)
    {
        var dictValueInfo = Resolve(schema.AdditionalProperties);
        return new ResolvedTypeInfo()
        {
            TypeName = $"Dict[str, Optional[{dictValueInfo.TypeName}]]",
            StructureType = "Dictionary",
            SourceType = dictValueInfo.SourceType,
            Of = dictValueInfo,
            Value = ResolveExample(schema.Example),
        };
    }

    protected override ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema)
    {
        return new ResolvedTypeInfo()
        {
            TypeName = "Any",
            SourceType = "Any",
            StructureType = "Any",
        };
    }

    protected override ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema)
    {
        var type = schema.Type switch
        {
            "string" => "StrictStr",
            "integer" => "StrictInt",
            "number" => "StrictFloat",
            "boolean" => "StrictBool",
            _ => "Any"
        };
        
        return new ResolvedTypeInfo()
        {
            TypeName = type,
            StructureType = "Primitive",
            SourceType = type,
            Value = ResolveExample(schema.Example),
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
            OpenApiBoolean boolValue => boolValue.Value.ToString(),
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