using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.TypeScript.Models;

public class TypeScriptIndexBinding : LiquidFileBinding
{
    public List<LiquidFileBinding> Types { get; set; } = new();
    
    public TypeScriptIndexBinding(string name) : base(name)
    {
    }
}