namespace OpenApiGenerator.CodeGen.Core.Models;

public class LiquidDtoInheritanceBinding : LiquidFileBinding
{
    public LiquidDtoInheritanceBinding(string name) : base(name)
    {
    }
    
    public string DiscriminatorValue { get; set; }
}