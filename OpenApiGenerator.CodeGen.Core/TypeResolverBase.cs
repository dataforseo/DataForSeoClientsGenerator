using System.Diagnostics;
using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Core;

public class ResolvedTypeInfo
{
    public string TypeName { get; set; }
    public string SourceType { get; set; }
    public IResolvedTypeValue Value { get; set; } //array of objects to include values of any structure type: array, object, dict etc.
    public string StructureType { get; set; } //structure type
    public ResolvedTypeInfo Of { get; set; }

    public bool HasExamples => !Value.IsEmpty;

    public ResolvedTypeInfo Clone()
    {
        return new ResolvedTypeInfo
        {
            TypeName = TypeName,
            Value = Value?.Clone(),
            StructureType = StructureType,
            Of = Of?.Clone(),
            SourceType = SourceType
        };
    }
}

public enum ResolvedTypeValueKind
{
    Primitive,
    Object,
    Array
}

public interface IResolvedTypeValue
{
    public ResolvedTypeValueKind Type { get; }
    public string FieldName { get; set; }
    public bool IsEmpty { get; }
    IResolvedTypeValue Clone();
}

public class ResolvedTypeArrayValueInfo : IResolvedTypeValue
{
    public ResolvedTypeValueKind Type { get; } = ResolvedTypeValueKind.Array;
    public ICollection<IResolvedTypeValue> Items { get; set; }
    public string FieldName { get; set; }
    public bool IsEmpty => Items.Count == 0;

    public IResolvedTypeValue Clone()
    {
        return new ResolvedTypeArrayValueInfo
        {
            FieldName = FieldName,
            Items = Items?.Select(item => item.Clone()).ToList()
        };
    }
}

public class ResolvedTypeObjectValueInfo : IResolvedTypeValue
{
    public ResolvedTypeValueKind Type { get; } = ResolvedTypeValueKind.Object;
    public List<ResolvedTypeObjectFieldInfo> Fields { get; set; }
    public bool IsEmpty => Fields.Count == 0;

    public string FieldName { get; set; }

    public IResolvedTypeValue Clone()
    {
        return new ResolvedTypeObjectValueInfo
        {
            FieldName = FieldName,
            Fields = Fields?
                .Select(x => 
                    new ResolvedTypeObjectFieldInfo()
                    {
                        FieldName = x.FieldName, 
                        Value = x.Value.Clone()
                    })
                .ToList()
        };
    }
}

public class ResolvedTypeObjectFieldInfo
{
    public string FieldName { get; set; }
    public IResolvedTypeValue Value { get; set; }
}

public class ResolvedTypePrimitiveValueInfo : IResolvedTypeValue
{
    public ResolvedTypeValueKind Type { get; } = ResolvedTypeValueKind.Primitive;
    public string FieldName { get; set; }
    public string Value { get; set; }
    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public IResolvedTypeValue Clone()
    {
        return new ResolvedTypePrimitiveValueInfo
        {
            FieldName = FieldName, 
            Value = Value
        };
    }
}

public abstract class TypeResolver
{
    protected readonly CodeGeneratorSettingsBase Settings;
    
    public TypeResolver(CodeGeneratorSettingsBase settings)
    {
        Settings = settings;
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
    
    protected virtual IResolvedTypeValue ResolveExample(IOpenApiAny example)
    {
        if (example is OpenApiArray array && array.Count > 0)
        {
            return new ResolvedTypeArrayValueInfo()
            {
                Items = array.Select(x => ResolveExample(x)).ToList()
            };
        }
        
        if (example is OpenApiObject obj)
        {
            return new ResolvedTypeObjectValueInfo()
            {
                Fields = obj.Select(x => new ResolvedTypeObjectFieldInfo()
                {
                    FieldName = Settings.PropertyNameResolver.Resolve(x.Key),
                    Value = ResolveExample(x.Value)
                }).ToList()
            };
        }

        return new ResolvedTypePrimitiveValueInfo()
        {
            Value = ExtractPrimitiveExample(example)
        };
    }
    
    protected abstract ResolvedTypeInfo ResolveArray(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveReference(OpenApiSchema schema);
    // protected abstract string ResolveInheritedClass(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveDicionary(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolveAnyObject(OpenApiSchema schema);
    protected abstract ResolvedTypeInfo ResolvePrimitiveType(OpenApiSchema schema);
    protected abstract string ExtractPrimitiveExample(IOpenApiAny schema);
}