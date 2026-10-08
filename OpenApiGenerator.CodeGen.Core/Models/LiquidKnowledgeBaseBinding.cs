namespace OpenApiGenerator.CodeGen.Core.Models;

public class LiquidKnowledgeBaseApiBinding : LiquidFileBinding
{
    public LiquidKnowledgeBaseApiBinding(string name) : base(name)
    {
    }

    public string ApiName { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string RelativePath { get; set; }
    public List<LiquidOperationBinding> Examples { get; set; } = [];
    public List<LiquidOperationBinding> Operations { get; set; } = [];
}

public class LiquidKnowledgeBaseSkillBinding : LiquidFileBinding
{
    public LiquidKnowledgeBaseSkillBinding(string name) : base(name)
    {
    }

    public List<LiquidKnowledgeBaseApiBinding> Apis { get; set; } = [];
}
