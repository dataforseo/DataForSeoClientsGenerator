using CodeGenerator.Core;
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
                        foreach (var (name, propSchema) in Document.Components.Schemas[refId].Properties)
                        {
                            var item = new LiquidPropertyBinding
                            {
                                Name = Settings.PropertyNameResolver.Resolve(name),
                                JsonName = name,
                                Type = Settings.TypeResolver.Resolve(propSchema),
                                IsRequired = propSchema.Nullable is not true,
                                Description = propSchema.Description,
                            };

                            if (!item.Type.HasExamples)
                                continue;
                            
                            operationBinding.Payload.Add(item);
                        }
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
        }

        //process dto
        foreach (var (name, schema) in Document.Components.Schemas)
        {
            var dtoCodeBinding = new LiquidDtoBinding(name);
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
                if (dependedTypeBinding != null && apiBinding.DependentTypes.All(x => x.ClassName != dependedTypeBinding.ClassName))
                    apiBinding.DependentTypes.Add(dependedTypeBinding);

                var responseDependedTypeBinding = SetupDependentTypes(method.ResponseType.TypeName);
                if (responseDependedTypeBinding != null && apiBinding.DependentTypes.All(x => x.ClassName != responseDependedTypeBinding.ClassName))
                    apiBinding.DependentTypes.Add(responseDependedTypeBinding);
            }
            
            //add dependent types for tests
            var testBinding = pool.OfType<LiquidApiTestsBinding>().FirstOrDefault(x => x.ApiName == apiBinding.ClassName);
            if (testBinding != null)
                testBinding.DependentTypes.AddRange(apiBinding.DependentTypes);
            
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
                foreach (var property in parent.Properties)
                {
                    var parentDependedTypeBinding = SetupDependentTypes(property.Type.TypeName);
                    if (parentDependedTypeBinding != null && parentDependedTypeBinding.ClassName != dtoBinding.ClassName)
                        dtoBinding.DependentTypes.Add(parentDependedTypeBinding);
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
                    JsonName = propertyName
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
}