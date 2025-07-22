namespace OpenApiGenerator.CodeGen.Core.Models;

public class LiquidPropertyBinding
{
    public string Name { get; set; }
    public string JsonName { get; set; }
    public string Description { get; set; }
    public ResolvedTypeInfo Type { get; set; }
    public bool IsRequired { get; set; }

    public LiquidPropertyBinding Clone()
    {
        return new LiquidPropertyBinding
        {
            Name = Name,
            JsonName = JsonName,
            Description = Description,
            Type = Type?.Clone(),
            IsRequired = IsRequired
        };
    }
}