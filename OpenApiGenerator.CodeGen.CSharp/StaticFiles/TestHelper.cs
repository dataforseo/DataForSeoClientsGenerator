using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataForSeo.Client.Tests.Helpers;

public class TestHelper
{
     public static void CheckAdditionalProperties(object obj, string path = "")
    {
        if (obj == null)
            return;

        Type type = obj.GetType();
        
        if (IsPrimitive(obj))
            return;

        if (obj is JToken)
            return;
        
        if (type.IsAssignableTo(typeof(IEnumerable)) && obj is IEnumerable collection)
        {
            foreach (var item in collection)        
                CheckAdditionalProperties(item);
            return;
        }
        
        var properties = type.GetProperties();
        var typeNameProperty = properties.FirstOrDefault(p => p.Name == "Type");
        if (typeNameProperty != null)
        {
            var typeName = typeNameProperty.GetValue(obj) as string;
            path = string.IsNullOrEmpty(path) ? path : $"{path}:{typeName}";
        }
        
        foreach (PropertyInfo property in properties)
        {
            string newPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";

            var propValue = property.GetValue(obj);
            
            if (property.Name == "AdditionalProperties")
                Assert.That(propValue, Is.Null.Or.Empty, $"Failed at path: {newPath}");
            
            try
            {
                var value = property.GetValue(obj);
                if (IsPrimitive(value))
                    continue;

                if (value is IDictionary dictionary)
                {
                    foreach (var key in dictionary.Keys)
                    {
                        CheckAdditionalProperties(dictionary[key], $"{newPath}[{key}]");
                    }
                }
                else if (value is IEnumerable enumerable && !(value is string))
                {
                    int index = 0;
                    foreach (var item in enumerable)
                    {
                        CheckAdditionalProperties(item, $"{newPath}[{index}]");
                        index++;
                    }
                }

                else
                {
                    CheckAdditionalProperties(value, newPath);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
          
        }

        bool IsPrimitive(object obj) => obj is int or long or double or float or decimal or bool or string or DateTime
            or DateTimeOffset
            or Guid or Enum;
    }
}