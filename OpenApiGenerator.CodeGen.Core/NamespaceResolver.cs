using CodeGenerator.Core;
using OpenApiGenerator.CodeGen.Core.Models;

namespace OpenApiGenerator.CodeGen.Core;

public abstract class NamespaceResolver
{
    public abstract CodeGeneratorSettingsBase Settings { get; }
    
    public abstract void ResolveNamespace(LiquidFileBinding binding);
    public abstract string ResolveFilePath(LiquidFileBinding binding);
}