namespace OpenApiGenerator.CodeGen.Core.Models;

public class LiquidDocumentationBinding : LiquidFileBinding
{
    public LiquidDocumentationBinding(string name) : base(name)
    {
    }
}

public class LiquidDocumentationDtoBinding : LiquidDocumentationBinding
{
    public LiquidDocumentationDtoBinding(string name) : base(name)
    {
    }

    public List<LiquidPropertyBinding> Properties { get; set; } = [];
}

public class LiquidDocumentationApiBinding : LiquidDocumentationBinding
{
    public LiquidDocumentationApiBinding(string name) : base(name)
    {
    }

    public List<LiquidOperationBinding> Operations { get; set; } = [];
}