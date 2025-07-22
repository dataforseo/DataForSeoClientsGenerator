using CodeGenerator.Core;

namespace OpenApiGenerator.CodeGen.Core.Models;

public abstract class LiquidBinding
{
}

public class LiquidFileBinding : LiquidBinding
{
    
    public LiquidFileBinding(string name)
    {
        ClassName = name;
    }
    
    public string Namespace { get; set; }
    public string ClassName { get; set; }
    
    public string FilePath { get; set; }
}

public class LiquidResourceFileBinding : LiquidFileBinding
{
    public LiquidResourceFileBinding(string name) : base(name)
    {
    }
    
    public string Version { get; set; }
    
    public string Path { get; set; }
    public string FileType { get; set; }
}