using CodeGenerator.Core;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Core;

public class ResolvedTypeInfo
{
    public string TypeName { get; set; }
    public string SourceType { get; set; }
    public List<ResolvedTypeValueInfo> Value { get; set; } //array of objects to include values of any structure type: array, object, dict etc.
    public string StructureType { get; set; } //structure type
    public ResolvedTypeInfo Of { get; set; }

    public bool HasExamples
    {
        get
        {
            return StructureType switch
            {
                "Primitive" => Value is { Count: > 0 },
                "Object" => Value is { Count: > 0 },
                "Dictionary" or "Array" => Value is { Count: > 0 } || Of.HasExamples,
                _ => false
            };
        }
    }

    public ResolvedTypeInfo Clone()
    {
        return new ResolvedTypeInfo
        {
            TypeName = TypeName,
            Value = Value?.Select(x => new ResolvedTypeValueInfo { Name = x.Name, Value = x.Value })?.ToList(),
            StructureType = StructureType,
            Of = Of?.Clone(),
            SourceType = SourceType
        };
    }
}

public class ResolvedTypeValueInfo
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public abstract class TypeResolver
{
    protected readonly CodeGeneratorSettingsBase SettingsBase;
    
    public TypeResolver(CodeGeneratorSettingsBase settings)
    {
        SettingsBase = settings;
    }

    public ResolvedTypeInfo Resolve(OpenApiSchema schema)
    {
        if (schema.Type == "array")
            return ResolveArray(schema);
        
        if (schema.IsAnyObjectSchema())
            return ResolveAnyObject(schema);
        
        if (schema.AdditionalProperties != null)
            return ResolveDicionary(schema);
        
        var schemaReference = schema.ReferenceId();
        if (!string.IsNullOrEmpty(schemaReference))
            return ResolveReference(schema);

        return ResolvePrimitiveType(schema);
    }
    
    protected abstract ResolvedTypeInfo ResolveArray(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveReference(OpenApiSchema schema);
    // protected abstract string ResolveInheritedClass(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveDicionary(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema);
}