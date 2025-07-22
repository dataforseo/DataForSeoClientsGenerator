using CodeGenerator.Core;

namespace OpenApiGenerator.CodeGen.Core;

public interface IResourceFactory
{
    LiquidTemplate CreateTemplate(LiquidConfig config);

    string ReadRawEmbeddedResource(string resourceName);
}