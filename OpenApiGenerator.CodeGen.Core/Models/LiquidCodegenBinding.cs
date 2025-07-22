namespace OpenApiGenerator.CodeGen.Core.Models;

public abstract class LiquidCodegenBinding : LiquidFileBinding
{
    public abstract string BindingType { get; set; }
    
    public LiquidCodegenBinding(string name) : base(name)
    {
    }
}

public class LiquidApiBinding : LiquidCodegenBinding
{
    public override string BindingType { get; set; } = "api";
    
    public LiquidApiBinding (string name) : base(name)
    {
    }
    
    public ICollection<LiquidOperationBinding> Operations { get; set; } = [];
    public List<LiquidDtoBinding> DependentTypes { get; set; } = [];
}

public class LiquidApiTestsBinding : LiquidCodegenBinding
{
    public override string BindingType { get; set; } = "test";
    
    public string ApiName { get; set; }
    
    public LiquidApiTestsBinding(string name) : base(name)
    {
    }
    
    public ICollection<LiquidOperationBinding> Operations { get; set; } = [];
    public List<LiquidDtoBinding> DependentTypes { get; set; } = [];
}

public class LiquidDtoBinding : LiquidCodegenBinding
{
    public override string BindingType { get; set; } = "dto";
    
    public LiquidDtoBinding(string name) : base(name)
    {
    }

    public bool IsParent { get; set; }
    public string DiscriminatorProperty { get; set; }
    public string DiscriminatorValue { get; set; }
    public string ParentName { get; set; }
    public LiquidDtoBinding Parent { get; set; }
    public List<LiquidDtoBinding> Childs { get; set; } = [];
    public Dictionary<string, string> ChildNames { get; set; } = [];
    public List<LiquidPropertyBinding> Properties { get; set; } = [];
    public List<string> DependentTypeNames { get; set; } = [];
    public List<LiquidDtoBinding> DependentTypes { get; set; } = [];
}