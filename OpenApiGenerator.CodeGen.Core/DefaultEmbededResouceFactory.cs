using System.Reflection;
using OpenApiGenerator.CodeGen.Core;

namespace CodeGenerator.Core;

public class DefaultEmbededResouceFactory : IResourceFactory
{
    private readonly CodeGeneratorSettingsBase _settings;
    private readonly Assembly[] _assemblies;

    public DefaultEmbededResouceFactory(CodeGeneratorSettingsBase settings, Assembly[] assemblies)
    {
        _settings = settings;
        _assemblies = assemblies;
    }

    public LiquidTemplate CreateTemplate(LiquidConfig config)
    {
        return new LiquidTemplate(
            config,
            (name) => GetLiquidTemplate(name),
            _settings);
    }
    
    public string ReadRawEmbeddedResource(string resourceName)
    {
        var root = "OpenApiGenerator.CodeGen." + _settings.Language;
        var assembly = GetLiquidAssembly(root); 
        var resource = assembly.GetManifestResourceStream($"{root}.{resourceName}");
        if (resource != null)
        {
            using (var reader = new StreamReader(resource))
            {
                return reader.ReadToEnd();
            }
        }

        return null;
    }

    private string GetLiquidTemplate(string template)
    {
        if (!template.EndsWith('!') && !string.IsNullOrEmpty(_settings.TemplateDirectory))
        {
            var templateFilePath = Path.Combine(_settings.TemplateDirectory, template + ".liquid");
            if (File.Exists(templateFilePath))
                return File.ReadAllText(templateFilePath);
        }

        return GetEmbeddedLiquidTemplate(template);
    }

    protected virtual string GetEmbeddedLiquidTemplate(string template)
    {
        template = template.TrimEnd('!');
        var resourceName = "Templates." + template + ".liquid";

        var resource = ReadRawEmbeddedResource(resourceName);
        if (resource == null)
            throw new InvalidOperationException("Could not load template '" + template + "' for language '" + _settings.Language + "'.");
        
        return resource;
    }

    protected Assembly GetLiquidAssembly(string name)
    {
        var assembly = _assemblies.FirstOrDefault(a => a.FullName.Contains(name));
        if (assembly != null)
        {
            return assembly;
        }

        throw new InvalidOperationException("The assembly '" + name + "' containting liquid templates could not be found.");
    }
}