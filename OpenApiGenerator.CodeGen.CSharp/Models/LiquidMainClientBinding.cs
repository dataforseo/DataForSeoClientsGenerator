using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.CSharp.Models;

public class LiquidMainClientBinding :  LiquidFileBinding
{
    public LiquidMainClientBinding(string name) : base(name)
    {
    }

    public HashSet<string> ApiList { get; set; } = [];
    public string Version { get; set; }
}