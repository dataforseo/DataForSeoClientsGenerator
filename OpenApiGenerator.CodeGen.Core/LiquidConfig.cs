namespace OpenApiGenerator.CodeGen.Core;

public class LiquidConfig
{
    public string TemplateName { get; set; }
    public object Bind { get; set; }

    public static LiquidConfig Create(string templateName, object bindingModel)
    {
        return new LiquidConfig
        {
            TemplateName = templateName,
            Bind = bindingModel
        };
    }
}