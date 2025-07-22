using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using CodeGenerator.Core;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using OpenApiGenerator.Utils.Extensions;
using Parlot.Fluent;

namespace OpenApiGenerator.CodeGen.Core;

public class LiquidTemplate
{
    internal const string TemplateTagName = "template";
    private static readonly ConcurrentDictionary<(string, string), IFluidTemplate> Templates = new();

    static LiquidTemplate()
    {
        // thread-safe
        _parser = new LiquidParser();
        _templateOptions = new TemplateOptions
        {
            MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
            CultureInfo = CultureInfo.InvariantCulture,
            Greedy = false,
        };
        _templateOptions.Filters.AddFilter("csharpdocs", LiquidFilters.Csharpdocs);
        _templateOptions.Filters.AddFilter("javadocs", LiquidFilters.Javadocs);
        _templateOptions.Filters.AddFilter("pythondocs", LiquidFilters.Onerow);
        _templateOptions.Filters.AddFilter("onerow", LiquidFilters.Onerow);

        _templateOptions.Filters.AddFilter("join", LiquidFilters.Join);
        _templateOptions.Filters.AddFilter("select", LiquidFilters.SelectField);

        _templateOptions.Filters.AddFilter("camelcase", LiquidFilters.CamelCase);
        _templateOptions.Filters.AddFilter("snakecase", LiquidFilters.SnakeCase);
        _templateOptions.Filters.AddFilter("pascalcase", LiquidFilters.PascalCase);
        _templateOptions.Filters.AddFilter("screamingcase", LiquidFilters.ScreamingCase);
    }

    private readonly LiquidConfig _liquidConfig;
    private readonly Func<string, string> _templateContentLoader;
    private readonly CodeGeneratorSettingsBase _settingsBase;

    private static readonly LiquidParser _parser;
    private static readonly TemplateOptions _templateOptions;

    private static readonly Regex _tabCountRegex = new("(\\s*)?\\{%(-)?\\s+template\\s+([a-zA-Z0-9_.]+)(\\s*?.*?)\\s(-)?%}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _csharpDocsRegex = new("(\n( )*)([^\n]*?) \\| csharpdocs }}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex _tabRegex = new("(\n( )*)([^\n]*?) \\| tab }}", RegexOptions.Singleline | RegexOptions.Compiled);

    public LiquidTemplate(
        LiquidConfig config,
        Func<string, string> templateContentLoader,
        CodeGeneratorSettingsBase settings)
    {
        _liquidConfig = config;
        _templateContentLoader = templateContentLoader;
        _settingsBase = settings;
    }

    public string Render()
    {
        var childScope = false;
        TemplateContext? templateContext = null;

        try
        {
            // use language and template name as key for faster lookup than using the content
            var key = (_settingsBase.Language.ToString(), _liquidConfig.TemplateName);
            var template = Templates.GetOrAdd(key, _ =>
            {
                // our matching expects unix new lines
                var templateContent = _templateContentLoader(_liquidConfig.TemplateName);
                var data = templateContent.Replace("\r", "");
                data = "\n" + data;

                // tab count parameters to template based on surrounding code, how many spaces before the template tag
                data = _tabCountRegex.Replace(data,
                    m =>
                    {
                        var whitespace = m.Groups[1].Value;

                        var rewritten = whitespace + "{%" + m.Groups[2].Value + " " + TemplateTagName;
                        // make te parameter a string literal as it's more valid and faster to process
                        rewritten += " '" + m.Groups[3].Value + "' ";

                        if (whitespace.Length > 0 && whitespace[0] == '\n')
                        {
                            // we can checks how many spaces
                            var tabCount = whitespace.TrimStart('\n').Length;
                            rewritten += tabCount + " ";
                        }

                        rewritten += m.Groups[5].Value + "%}";

                        return rewritten;
                    });

                data = _csharpDocsRegex.Replace(data, m =>
                    m.Groups[1].Value + m.Groups[3].Value + " | csharpdocs: " + m.Groups[1].Value.Length / 4 + " }}");

                data = _tabRegex.Replace(data, m =>
                    m.Groups[1].Value + m.Groups[3].Value + " | tab: " + m.Groups[1].Value.Length / 4 + " }}");

                return _parser.Parse(data);
            });

            if (_liquidConfig.Bind is TemplateContext outerContext)
            {
                // we came from template call
                templateContext = outerContext;
                templateContext.EnterChildScope();
                childScope = true;
            }
            else
            {
                templateContext = new TemplateContext(_liquidConfig.Bind ?? new object(), _templateOptions);
                templateContext.AmbientValues.Add(LiquidParser.SettingsKey, _settingsBase);
            }

            templateContext.AmbientValues[LiquidParser.LanguageKey] = _settingsBase.Language;
            templateContext.AmbientValues[LiquidParser.TemplateKey] = _liquidConfig.TemplateName;

            var render = template.Render(templateContext);
            var trimmed = render.Replace("\r", "").Trim('\n');

            // clean up cases where we have called template but it produces empty output
            var withoutEmptyWhiteSpace = Regex.Replace(trimmed, @"^[ ]+__EMPTY-TEMPLATE__$[\n]{0,1}", string.Empty, RegexOptions.Multiline);

            // just to make sure we don't leak out marker
            return withoutEmptyWhiteSpace.Replace("__EMPTY-TEMPLATE__", "");
        }
        catch (Exception exception)
        {
            var message = $"Error while rendering Liquid template {_settingsBase.Language}/{_liquidConfig.TemplateName}: \n{exception.Message}";
            if (exception.Message.Contains("'{% endif %}' was expected ") && exception.Message.Contains("elseif"))
            {
                message += ", did you use 'elseif' instead of correct 'elsif'?";
            }

            throw new InvalidOperationException(message, exception);
        }
        finally
        {
            if (childScope)
            {
                templateContext?.ReleaseScope();
            }
        }
    }
}

static class LiquidFilters
{
    private static List<string> SyntaxKeys = new()
    {
        "default"
    };

    public static ValueTask<FluidValue> Csharpdocs(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var tabCount = (int)arguments.At(0).ToNumberValue();
        var converted = ConversionUtilities.ConvertCSharpDocs(input.ToStringValue(), tabCount);
        return new ValueTask<FluidValue>(new StringValue(converted));
    }

    public static async ValueTask<FluidValue> SelectField(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type != FluidValues.Array)
            return input;

        var items = input.Enumerate(context).ToList();
        var fieldName = arguments.At(0).ToStringValue();
        var filterName = arguments.Count > 1 ? arguments.At(1).ToStringValue() : null;

        var resultItems = new List<FluidValue>();
        foreach (var item in items)
        {
            var obj = item.ToObjectValue();
            var property = obj?.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                continue;

            var rawValue = property.GetValue(obj);
            var fluidValue = FluidValue.Create(rawValue, context.Options);

            if (!string.IsNullOrEmpty(filterName) && context.Options.Filters.TryGetValue(filterName, out var filter))
                fluidValue = await filter(fluidValue, new FilterArguments(), context);

            resultItems.Add(fluidValue);
        }

        return new ArrayValue(resultItems);
    }

    public static ValueTask<FluidValue> Join(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type != FluidValues.Array)
            return new ValueTask<FluidValue>(input);

        var separator = arguments.At(0).ToStringValue();
        var items = input.Enumerate(context).ToList();

        var parts = items.Select(item => item.ToStringValue()).ToList();

        var result = string.Join(separator, parts);
        return new ValueTask<FluidValue>(new StringValue(result));
    }

    public static async ValueTask<FluidValue> JoinFields(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type != FluidValues.Array)
            return input;

        var items = input.Enumerate(context).ToList();
        var fieldName = arguments.At(0).ToStringValue();
        var separator = arguments.At(1).ToStringValue();
        var filterName = arguments.Count > 2 ? arguments.At(2).ToStringValue() : null;

        var parts = new List<string>();
        foreach (var item in items)
        {
            var obj = item.ToObjectValue();
            var property = obj?.GetType().GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                continue;

            var rawValue = property.GetValue(obj);

            var fluidValue = FluidValue.Create(rawValue, context.Options);
            if (!string.IsNullOrEmpty(filterName) && context.Options.Filters.TryGetValue(filterName, out var filter))
                fluidValue = await filter(fluidValue, new FilterArguments(), context);

            var val = fluidValue.ToStringValue();
            if (!string.IsNullOrEmpty(val))
                parts.Add(val);
        }

        var result = string.Join(separator, parts);
        return new StringValue(result);
    }


    public static ValueTask<FluidValue> Javadocs(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var tabCount = (int)arguments.At(0).ToNumberValue();
        var converted = ConversionUtilities.ConvertJavaDocs(input.ToStringValue(), tabCount);
        return new ValueTask<FluidValue>(new StringValue(converted));
    }


    public static ValueTask<FluidValue> Onerow(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        var converted = input.ToStringValue().Replace("\n", ". ");
        return new ValueTask<FluidValue>(new StringValue(converted));
    }

    public static ValueTask<FluidValue> CamelCase(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type == FluidValues.Array)
        {
            var items = input
                .Enumerate(context)
                .Select(x =>
                {
                    var converted = x.ToStringValue().ToCamelCase();
                    if (SyntaxKeys.Contains(converted))
                        converted = "_" + converted;
                    return converted;
                })
                .Select(x => new StringValue(x))
                .ToList();

            return new ValueTask<FluidValue>(new ArrayValue(items));
        }

        var converted = input.ToStringValue().ToCamelCase();

        if (SyntaxKeys.Contains(converted))
            converted = "_" + converted;

        return new ValueTask<FluidValue>(new StringValue(converted));
    }

    public static ValueTask<FluidValue> SnakeCase(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type == FluidValues.Array)
        {
            var items = input
                .Enumerate(context)
                .Select(x =>
                {
                    var converted = x.ToStringValue().ToSnakeCase();
                    if (SyntaxKeys.Contains(converted))
                        converted = "_" + converted;
                    return converted;
                })
                .Select(x => new StringValue(x))
                .ToList();

            return new ValueTask<FluidValue>(new ArrayValue(items));
        }

        var converted = input.ToStringValue().ToSnakeCase();

        if (SyntaxKeys.Contains(converted))
            converted = "_" + converted;

        return new ValueTask<FluidValue>(new StringValue(converted));
    }

    public static ValueTask<FluidValue> PascalCase(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type == FluidValues.Array)
        {
            var items = input
                .Enumerate(context)
                .Select(x =>
                {
                    var converted = x.ToStringValue().ToPascalCase();
                    if (SyntaxKeys.Contains(converted))
                        converted = "_" + converted;
                    return converted;
                })
                .Select(x => new StringValue(x))
                .ToList();

            return new ValueTask<FluidValue>(new ArrayValue(items));
        }

        var converted = input.ToStringValue().ToPascalCase();

        if (SyntaxKeys.Contains(converted))
            converted = "_" + converted;

        return new ValueTask<FluidValue>(new StringValue(converted));
    }

    public static ValueTask<FluidValue> ScreamingCase(FluidValue input, FilterArguments arguments, TemplateContext context)
    {
        if (input.Type == FluidValues.Array)
        {
            var items = input
                .Enumerate(context)
                .Select(x => x.ToStringValue().ToScreamingCase())
                .Select(x => new StringValue(x))
                .ToList();

            return new ValueTask<FluidValue>(new ArrayValue(items));
        }

        var converted = input.ToStringValue().ToScreamingCase();
        return new ValueTask<FluidValue>(new StringValue(converted));
    }
}

sealed class LiquidParser : FluidParser
{
    internal const string LanguageKey = "__language";
    internal const string TemplateKey = "__template";
    internal const string SettingsKey = "__settings";

    public LiquidParser()
    {
        RegisterParserTag(LiquidTemplate.TemplateTagName, Parsers.OneOrMany(Primary), RenderTemplate);
        RegisterParserBlock("with", Parsers.OneOrMany(Primary), async (arguments, statements, writer, encoder, context) =>
        {
            if (arguments.Count == 0)
                return Completion.Normal;

            // Зберігаємо оригінальну модель
            var originalModel = context.Model;

            try
            {
                // Обчислюємо нову модель
                var newModel = await arguments[0].EvaluateAsync(context);
                
                // Підміняємо модель в контексті
                // context.Model = newModel.ToObjectValue();
                var newContext = new TemplateContext(newModel, context.Options);
                foreach (var (ambientKey, ambientValue) in context.AmbientValues)
                    newContext.AmbientValues[ambientKey] = ambientValue;
                
                // Рендеримо вміст блоку
                for (var i = 0; i < statements.Count; i++)
                {
                    var completion = await statements[i].WriteToAsync(writer, encoder, newContext);
                    if (completion != Completion.Normal)
                    {
                        return completion;
                    }
                }
                
                return Completion.Normal;
            }
            finally
            {
                // // Відновлюємо оригінальну модель
                // context.Model = originalModel;
            }
        });
    }
    
    // private static Зфкіук

    private static ValueTask<Completion> RenderTemplate(
        IReadOnlyList<Expression> arguments,
        TextWriter writer,
        TextEncoder encoder,
        TemplateContext context)
    {
        var templateName = ((LiteralExpression)arguments[0]).Value.ToStringValue();
        object withModel = null;

        // 🗝️ Перевірити чи є `with`
        if (arguments.Count > 1 && arguments[1] is MemberExpression expr)
        {
            withModel = expr.EvaluateAsync(context).Result.ToObjectValue();
        }
        var tabCount = -1;
        if (arguments.Count > 1 && arguments[1] is LiteralExpression literalExpression)
        {
            tabCount = (int)literalExpression.Value.ToNumberValue();
        }

        var settings = (CodeGeneratorSettingsBase)context.AmbientValues[SettingsKey];
        templateName = !string.IsNullOrEmpty(templateName)
            ? templateName
            : (string)context.AmbientValues[TemplateKey] + "!";

        var template = settings.EmbededResouceFactory.CreateTemplate(LiquidConfig.Create(templateName, context));
        var output = template.Render().Trim('\n').Trim();

        if (string.IsNullOrWhiteSpace(output))
        {
            // signal cleanup
            writer.Write("__EMPTY-TEMPLATE__");
        }
        else if (tabCount > 0)
        {
            ConversionUtilities.Tab(output, tabCount, writer);
        }
        else
        {
            writer.Write(output);
        }

        return new ValueTask<Completion>(Completion.Normal);
    }
}

sealed class UnsafeMemberAccessStrategy : DefaultMemberAccessStrategy
{
    private readonly MemberAccessStrategy baseMemberAccessStrategy = new DefaultMemberAccessStrategy();

    public override IMemberAccessor GetAccessor(Type type, string name)
    {
        var accessor = baseMemberAccessStrategy.GetAccessor(type, name);
        if (accessor != null)
        {
            return accessor;
        }

        baseMemberAccessStrategy.Register(type);
        accessor = baseMemberAccessStrategy.GetAccessor(type, name);
        return accessor;
    }
}