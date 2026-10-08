using CodeGenerator.Core;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiGenerator.CodeGen.Core.Models;
using OpenApiGenerator.Utils.Extensions;

namespace OpenApiGenerator.CodeGen.Core;

public abstract class BaseCodeGenerator
{
    protected abstract CodeGeneratorSettingsBase Settings { get; }

    protected OpenApiDocument Document { get; set; }

    protected abstract List<string> SyntaxKeys { get; }

    public BaseCodeGenerator(OpenApiDocument document)
    {
        Document = document;
    }

    protected virtual List<LiquidBinding> CreateBindings()
    {
        var pool = new List<LiquidBinding>();
        var dtoBindings = new List<LiquidDtoBinding>();
        var apiBindings = new List<LiquidApiBinding>();
        var knowledgeBaseBindings = new List<LiquidKnowledgeBaseApiBinding>();
        foreach (var group in (Document.Paths ?? []).GroupBy(x => x.Value.Operations.Values.First().Tags.First().Name))
        {
            var apiName = Settings.ClassNameResolver.Resolve($"{group.Key} Api");

            var apiCodeBinding = new LiquidApiBinding(apiName);
            Settings.NamespaceResolver.ResolveNamespace(apiCodeBinding);
            Settings.NamespaceResolver.ResolveFilePath(apiCodeBinding);

            var apiDocBinding = new LiquidDocumentationApiBinding(apiName);
            Settings.NamespaceResolver.ResolveNamespace(apiDocBinding);
            Settings.NamespaceResolver.ResolveFilePath(apiDocBinding);
            
            var apiTestsBinding = new LiquidApiTestsBinding($"{apiName}Test")
            {
                ApiName = apiName
            };
            Settings.NamespaceResolver.ResolveNamespace(apiTestsBinding);
            Settings.NamespaceResolver.ResolveFilePath(apiTestsBinding);

            var description = ReadKnowledgeBaseDescription(apiName);
            var apiKnowledgeBaseBinding = new LiquidKnowledgeBaseApiBinding(apiName)
            {
                ApiName = apiName,
                Description = description,
                Summary = description?.Split("\n\n")[0].Replace("\n", " "),
            };
            Settings.NamespaceResolver.ResolveNamespace(apiKnowledgeBaseBinding);
            Settings.NamespaceResolver.ResolveFilePath(apiKnowledgeBaseBinding);
            var exampleCandidates = new List<(string Path, LiquidOperationBinding Operation)>();

            //enumerate api
            foreach (var (path, pathInfo) in group)
            {
                foreach (var (type, operationInfo) in pathInfo.Operations)
                {
                    var operationBinding = new LiquidOperationBinding
                    {
                        ApiName = apiName,
                        Path = path,
                        Host = Settings.Host,
                        Version = Settings.Version,
                        HttpMethod = type.ToString().ToUpper(),
                        OperationName = Settings.ApiMethodNameResolver.Resolve(operationInfo.OperationId),
                        ResponseType = Settings.TypeResolver.Resolve(operationInfo.Responses.First().Value.Content.First().Value.Schema)
                    };

                    if (type == OperationType.Post)
                    {
                        var payloadSchema = operationInfo.RequestBody.Content.First().Value.Schema;
                        operationBinding.RequestType = Settings.TypeResolver.Resolve(payloadSchema);
                        var refId = payloadSchema.ReferenceId();
                        operationBinding.Payload = [];

                        var examples = (payloadSchema.Example as OpenApiArray)?.FirstOrDefault() as OpenApiObject;
                        if (examples is not null
                            && !string.IsNullOrEmpty(refId)
                            && Document.Components.Schemas.TryGetValue(refId, out var objSchema))
                        {
                            foreach (var (name, value) in examples)
                            {
                                try
                                {
                                    if (!objSchema.Properties.TryGetValue(name, out var propSchema))
                                        continue;

                                    propSchema.Example = value;
                                    var item = new LiquidPropertyBinding
                                    {
                                        Name = Settings.PropertyNameResolver.Resolve(name),
                                        JsonName = name,
                                        Type = Settings.TypeResolver.Resolve(propSchema),
                                        IsRequired = propSchema.Nullable is not true,
                                        Description = propSchema.Description,
                                        IsDeprecated = propSchema.Deprecated,
                                    };

                                    BindExampleTypes(item.Type.Value, propSchema);
                                    RemoveEmptyExampleValues(item.Type.Value);
                                
                                    if (!item.Type.HasExamples)
                                        continue;
                            
                                    operationBinding.Payload.Add(item);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    continue;
                                }
                            }
                        }

                        foreach (var item in operationBinding.Payload)
                            CollectExampleTypes(item.Type.Value, operationBinding.ExampleTypes);
                    }

                    if (type == OperationType.Get)
                    {
                        var parameter = operationInfo.Parameters.FirstOrDefault();
                        if (parameter is not null)
                        {
                            parameter.Schema.Example = parameter.Example;
                            operationBinding.GetParameter = new LiquidPropertyBinding()
                            {
                                Name = parameter.Name,
                                Type = Settings.TypeResolver.Resolve(parameter.Schema)
                            };
                        }
                    }
                    
                    apiCodeBinding.Operations.Add(operationBinding);

                    var docOperationBinding = operationBinding.Clone();
                    docOperationBinding.Login = "USERNAME";
                    docOperationBinding.Password = "PASSWORD";
                    apiDocBinding.Operations.Add(docOperationBinding);
                    apiKnowledgeBaseBinding.Operations.Add(docOperationBinding);
                    exampleCandidates.Add((path, docOperationBinding));
                    
                    var testOperationBinding = operationBinding.Clone();
                    testOperationBinding.Host = Settings.Sandbox.Host;
                    testOperationBinding.Login = Settings.Sandbox.Login;
                    testOperationBinding.Password = Settings.Sandbox.Password;
                    testOperationBinding.UserAgent = Settings.Sandbox.UserAgent;
                    testOperationBinding.ForTests = true;
                    apiTestsBinding.Operations.Add(testOperationBinding);
                }
            }

            apiBindings.Add(apiCodeBinding);
            pool.Add(apiDocBinding);
            pool.Add(apiTestsBinding);

            apiKnowledgeBaseBinding.Examples = SelectKnowledgeBaseExamples(apiName, exampleCandidates);
            knowledgeBaseBindings.Add(apiKnowledgeBaseBinding);
            pool.Add(apiKnowledgeBaseBinding);
        }

        pool.Add(CreateKnowledgeBaseSkillBinding(knowledgeBaseBindings));

        //process dto
        foreach (var (name, schema) in Document.Components.Schemas)
        {
            var dtoCodeBinding = new LiquidDtoBinding(name)
            {
                IsDeprecated = schema.Deprecated
            };
            Settings.NamespaceResolver.ResolveNamespace(dtoCodeBinding);
            Settings.NamespaceResolver.ResolveFilePath(dtoCodeBinding);

            var dtoDocBinding = new LiquidDocumentationDtoBinding(name);
            Settings.NamespaceResolver.ResolveNamespace(dtoDocBinding);
            Settings.NamespaceResolver.ResolveFilePath(dtoDocBinding);

            if (schema.Discriminator is { Mapping: not null })
            {
                dtoCodeBinding.DiscriminatorProperty = schema.Discriminator.PropertyName;
                dtoCodeBinding.IsParent = true;
                foreach (var (value, schemaRef) in schema.Discriminator.Mapping)
                    dtoCodeBinding.ChildNames[value] = schemaRef.Split('/').Last(); //schema name
            }

            var properties = schema.Properties;
            if (schema.AllOf is { Count: 2 })
            {
                dtoCodeBinding.ParentName = schema.AllOf[0].ReferenceName();
                properties = schema.AllOf[1].Properties;
                dtoCodeBinding.IsDeprecated = dtoCodeBinding.IsDeprecated || schema.AllOf[1].Deprecated;
            }

            var propertyBindings = BindProperties(properties);

            dtoDocBinding.Properties.AddRange(
                propertyBindings
                    .Select(x => x.Clone())
                    .Select(x =>
                    {
                        if (!string.IsNullOrEmpty(x.Description))
                            x.Description = x.Description.Replace("\n", "<br>");
                        return x;
                    }));
            dtoCodeBinding.Properties.AddRange(propertyBindings);
            dtoCodeBinding.DependentTypeNames = HandleDependentTypes(properties.Values);
            dtoBindings.Add(dtoCodeBinding);
            pool.Add(dtoDocBinding);
        }

        //fill dependent types and other post-processing
        foreach (var apiBinding in apiBindings)
        {
            foreach (var method in apiBinding.Operations)
            {
                var dependedTypeBinding = SetupDependentTypes(method.RequestType?.Of?.TypeName.Replace("[]", ""));
                method.RequestTypeBinding = dependedTypeBinding;
                if (dependedTypeBinding != null && apiBinding.DependentTypes.All(x => x.ClassName != dependedTypeBinding.ClassName))
                {
                    apiBinding.DependentTypes.Add(dependedTypeBinding);

                    var items = dependedTypeBinding.DependentTypeNames
                        ?.Where(x => apiBinding.DependentTypes.All(xx => xx.ClassName != x))
                        .Select(x => dtoBindings.FirstOrDefault(xx => xx.ClassName == x))
                        .Where(x => x != null)
                        .ToList() ?? [];
                    
                    apiBinding.DependentTypes.AddRange(items);
                }

                var responseDependedTypeBinding = SetupDependentTypes(method.ResponseType.TypeName);
                method.ResponseTypeBinding = responseDependedTypeBinding;
                if (responseDependedTypeBinding != null && apiBinding.DependentTypes.All(x => x.ClassName != responseDependedTypeBinding.ClassName))
                    apiBinding.DependentTypes.Add(responseDependedTypeBinding);
            }
            
            //add dependent types for tests
            var testBinding = pool.OfType<LiquidApiTestsBinding>().FirstOrDefault(x => x.ApiName == apiBinding.ClassName);
            if (testBinding != null)
            {
                testBinding.DependentTypes.AddRange(apiBinding.DependentTypes);
                testBinding.ExampleTypes = apiBinding.Operations
                    .SelectMany(x => x.ExampleTypes)
                    .Distinct()
                    .Where(x => testBinding.DependentTypes.All(type => type.ClassName != x)
                                && dtoBindings.Any(type => type.ClassName == x))
                    .ToList();
            }
            
            pool.Add(apiBinding);
        }

        //fill dependent types and other post-processing
        foreach (var dtoBinding in dtoBindings)
        {
            dtoBinding.DependentTypes = [];
            foreach (var property in dtoBinding.Properties)
            {
                var dependedTypeBinding = SetupDependentTypes(
                    property.Type.StructureType == "Array" 
                        ? property.Type.Of.TypeName 
                        : property.Type.TypeName
                    );
                if (dependedTypeBinding != null && dependedTypeBinding.ClassName != dtoBinding.ClassName)
                    dtoBinding.DependentTypes.Add(dependedTypeBinding);
            }

            if (dtoBinding.ChildNames is { Count: > 0 })
            {
                foreach (var (discriminatorValue, className) in dtoBinding.ChildNames)
                {
                    var child = dtoBindings.FirstOrDefault(x => x.ClassName == className);
                    if (child == null)
                        continue;

                    child.Parent = dtoBinding;
                    child.DiscriminatorValue = discriminatorValue;
                    dtoBinding.Childs.Add(child);
                }
            }

            foreach (var dependentTypeName in dtoBinding.DependentTypeNames)
            {
                var dependedTypeBinding = SetupDependentTypes(dependentTypeName);
                if (dependedTypeBinding != null && dependedTypeBinding.ClassName != dtoBinding.ClassName)
                {
                    dtoBinding.DependentTypes.Add(dependedTypeBinding);
                }
            }
            
            if (!string.IsNullOrEmpty(dtoBinding.ParentName))
            {
                var dependedTypeBinding = SetupDependentTypes(dtoBinding.ParentName);
                if (dependedTypeBinding != null)
                    dtoBinding.DependentTypes.Add(dependedTypeBinding);

                var parent = dtoBindings.FirstOrDefault(x => x.ClassName == dtoBinding.ParentName);
                if (parent?.Properties != null)
                {
                    foreach (var property in parent.Properties)
                    {
                        var parentDependedTypeBinding = SetupDependentTypes(property.Type.TypeName);
                        if (parentDependedTypeBinding != null && parentDependedTypeBinding.ClassName != dtoBinding.ClassName)
                            dtoBinding.DependentTypes.Add(parentDependedTypeBinding);
                    }
                }
            }

            dtoBinding.DependentTypes = dtoBinding.DependentTypes.DistinctBy(x => x.ClassName).ToList();

            if (!string.IsNullOrEmpty(dtoBinding.ParentName))
            {
                var parent = dtoBindings.FirstOrDefault(x => x.ClassName == dtoBinding.ParentName);
                dtoBinding.Parent = parent;
            }
            
            pool.Add(dtoBinding);
        }

        return pool;

        LiquidDtoBinding SetupDependentTypes(string searchType)
        {
            if (string.IsNullOrEmpty(searchType))
                return null;

            var candidates = dtoBindings.Where(x => x.ClassName == searchType).ToList();
            return candidates.Count switch
            {
                0 => null,
                1 => candidates[0],
                _ => null
            };
        }
    }

    private LiquidKnowledgeBaseSkillBinding CreateKnowledgeBaseSkillBinding(List<LiquidKnowledgeBaseApiBinding> apis)
    {
        var skillBinding = new LiquidKnowledgeBaseSkillBinding("SKILL")
        {
            Apis = apis
        };
        Settings.NamespaceResolver.ResolveNamespace(skillBinding);
        Settings.NamespaceResolver.ResolveFilePath(skillBinding);

        var skillDirectory = Path.GetDirectoryName(skillBinding.FilePath) ?? string.Empty;
        foreach (var api in apis)
            api.RelativePath = Path.GetRelativePath(skillDirectory, api.FilePath).Replace('\\', '/');

        foreach (var configuredApi in Settings.KnowledgeBase?.Endpoints?.Keys ?? Enumerable.Empty<string>())
        {
            if (apis.All(x => !string.Equals(x.ApiName, configuredApi, StringComparison.OrdinalIgnoreCase)))
                Console.WriteLine($"[KnowledgeBase] API '{configuredApi}' from the configuration is not found");
        }

        return skillBinding;
    }

    private List<LiquidOperationBinding> SelectKnowledgeBaseExamples(
        string apiName,
        List<(string Path, LiquidOperationBinding Operation)> candidates)
    {
        var endpoints = Settings.KnowledgeBase?.Endpoints?
            .FirstOrDefault(x => string.Equals(x.Key, apiName, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (endpoints is not { Count: > 0 })
        {
            Console.WriteLine($"[KnowledgeBase] no endpoints configured for '{apiName}', its knowledge-base file has no examples");
            return [];
        }

        var examples = new List<LiquidOperationBinding>();
        foreach (var endpoint in endpoints)
        {
            var path = NormalizeKnowledgeBaseEndpoint(endpoint);
            if (path == null)
            {
                Console.WriteLine($"[KnowledgeBase] endpoint '{endpoint}' of '{apiName}' must be a path starting with '/' or an absolute URL");
                continue;
            }

            var matches = candidates
                .Where(x => string.Equals(x.Path.TrimEnd('/'), path, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Operation)
                .ToList();

            if (matches.Count == 0)
                Console.WriteLine($"[KnowledgeBase] endpoint '{endpoint}' is not found in '{apiName}'");

            examples.AddRange(matches.Where(x => !examples.Contains(x)));
        }

        return examples;
    }

    private static string NormalizeKnowledgeBaseEndpoint(string endpoint)
    {
        var value = endpoint?.Trim();
        if (string.IsNullOrEmpty(value))
            return null;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            value = Uri.UnescapeDataString(uri.AbsolutePath);

        return value.StartsWith('/') ? value.TrimEnd('/') : null;
    }

    private static string ReadKnowledgeBaseDescription(string apiName)
    {
        var assembly = typeof(BaseCodeGenerator).Assembly;
        var resourceSuffix = $".KnowledgeBase.{apiName}.md";
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (resourceName == null)
        {
            Console.WriteLine($"[KnowledgeBase] description 'KnowledgeBase/{apiName}.md' is not found");
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)!;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r", "").Trim();
    }

    //setup depended on types without additional info about item
    private List<string> HandleDependentTypes(ICollection<OpenApiSchema> properties)
    {
        return properties.Select(x => 
                x.ReferenceId()
                ?? x.AdditionalProperties?.ReferenceId()
                )
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();
    }

    private ICollection<LiquidPropertyBinding> BindProperties(IDictionary<string, OpenApiSchema> properties)
    {
        var bindings = new List<LiquidPropertyBinding>();
        foreach (var (propertyName, propertySchema) in properties)
        {
            var propName = Settings.PropertyNameResolver.Resolve(propertyName);
            if (SyntaxKeys != null && SyntaxKeys.Any(x => string.Compare(x, propName, StringComparison.OrdinalIgnoreCase) == 0))
            {
                propName = $"{propName}_";
            }

            try
            {
                bindings.Add(new()
                {
                    IsRequired = propertySchema.Nullable is not true,
                    Name = propName,
                    Type = Settings.TypeResolver.Resolve(propertySchema),
                    Description = propertySchema.Description?.Replace("\"", "'"),
                    JsonName = propertyName,
                    IsDeprecated = propertySchema.Deprecated
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return bindings;
    }

    private void BindExampleTypes(IResolvedTypeValue value, OpenApiSchema schema)
    {
        if (value is null || schema is null)
            return;

        if (value is ResolvedTypeArrayValueInfo arrayValue && schema.Type == "array")
        {
            foreach (var item in arrayValue.Items)
                BindExampleTypes(item, schema.Items);
            return;
        }

        if (value is ResolvedTypeObjectValueInfo unresolvedObjectValue)
            unresolvedObjectValue.SourceType = schema.ReferenceId();

        schema = ResolveSchema(schema);

        if (value is not ResolvedTypeObjectValueInfo objectValue)
            return;

        var properties = new Dictionary<string, OpenApiSchema>(schema.Properties ?? new Dictionary<string, OpenApiSchema>());

        if (schema.Discriminator?.Mapping is { Count: > 0 })
        {
            var childType = schema.Discriminator.Mapping
                .FirstOrDefault(mapping => objectValue.Fields.Any(field =>
                    string.Equals(field.JsonName, mapping.Key, StringComparison.OrdinalIgnoreCase)))
                .Value;

            if (!string.IsNullOrEmpty(childType))
            {
                var childName = childType.Split('/').Last();
                objectValue.SourceType = childName;
                // fluent setters inherited from the parent return the parent type (Java), so child-only fields go first
                var parentProperties = schema.Properties;
                objectValue.Fields = objectValue.Fields
                    .OrderBy(field => parentProperties?.ContainsKey(field.JsonName) == true)
                    .ToList();

                if (Document.Components.Schemas.TryGetValue(childName, out var childSchema))
                {
                    var childProperties = childSchema.AllOf is { Count: 2 }
                        ? childSchema.AllOf[1].Properties
                        : childSchema.Properties;
                    foreach (var (name, propertySchema) in childProperties ?? new Dictionary<string, OpenApiSchema>())
                        properties.TryAdd(name, propertySchema);
                }
            }
        }

        foreach (var field in objectValue.Fields)
        {
            if (!properties.TryGetValue(field.JsonName, out var fieldSchema))
                continue;

            field.Type = Settings.TypeResolver.Resolve(fieldSchema);
            BindExampleTypes(field.Value, fieldSchema);
        }
    }

    private static void RemoveEmptyExampleValues(IResolvedTypeValue value)
    {
        switch (value)
        {
            case ResolvedTypeArrayValueInfo arrayValue:
                foreach (var item in arrayValue.Items)
                    RemoveEmptyExampleValues(item);
                arrayValue.Items = arrayValue.Items.Where(item => item is { IsEmpty: false }).ToList();
                break;
            case ResolvedTypeObjectValueInfo objectValue:
                foreach (var field in objectValue.Fields)
                    RemoveEmptyExampleValues(field.Value);
                objectValue.Fields.RemoveAll(field => field.Value is null or { IsEmpty: true });
                break;
        }
    }

    private static void CollectExampleTypes(IResolvedTypeValue value, List<string> types)
    {
        switch (value)
        {
            case ResolvedTypeArrayValueInfo arrayValue:
                foreach (var item in arrayValue.Items)
                    CollectExampleTypes(item, types);
                break;
            case ResolvedTypeObjectValueInfo objectValue:
                if (!string.IsNullOrEmpty(objectValue.SourceType) && !types.Contains(objectValue.SourceType))
                    types.Add(objectValue.SourceType);
                foreach (var field in objectValue.Fields)
                    CollectExampleTypes(field.Value, types);
                break;
        }
    }

    private OpenApiSchema ResolveSchema(OpenApiSchema schema)
    {
        var referenceId = schema.ReferenceId();
        if (!string.IsNullOrEmpty(referenceId)
            && Document.Components.Schemas.TryGetValue(referenceId, out var referencedSchema))
            return referencedSchema;

        if (schema.OneOf is { Count: 1 })
            return ResolveSchema(schema.OneOf[0]);

        return schema;
    }
}