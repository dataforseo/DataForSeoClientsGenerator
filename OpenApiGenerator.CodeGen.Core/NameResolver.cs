using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Core;

public abstract class NameResolver
{
    public abstract string Resolve(string schema);
}

public class PascalCaseNameResolver : NameResolver
{
    public override string Resolve(string name)
    {
        return name.ToPascalCase();
    }
}

public class SnakeCaseNameResolver : NameResolver
{
    public override string Resolve(string name)
    {
        return name.ToSnakeCase();
    }
}

public class CamelCaseNameResolver : NameResolver
{
    public override string Resolve(string name)
    {
        return name.ToCamelCase();
    }
}