using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DataForSeo.Client.Tests.Helpers;

public class TestHelper
{
    public const int SandboxNoPreparedDataStatusCode = 40404;
    public const int RateLimitExceededStatusCode = 40202;

    private const int MaxRateLimitRetries = 5;
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromMinutes(1);
    private static readonly object RateLimitSync = new();
    private static DateTime _rateLimitResumeAtUtc = DateTime.MinValue;

    public static async Task<T> ExecuteWithSandboxFallback<T>(Func<Task<T>> request, Action useProductionApi)
    {
        var usedProductionFallback = false;
        var rateLimitRetries = 0;

        while (true)
        {
            await WaitIfRateLimitedAsync();
            var result = await request();

            if (HasStatusCode(result, RateLimitExceededStatusCode))
            {
                if (rateLimitRetries >= MaxRateLimitRetries)
                    return result;

                rateLimitRetries++;
                MarkRateLimited();
                TestContext.WriteLine(
                    $"Rate limit exceeded ({RateLimitExceededStatusCode}). Retrying in {RateLimitDelay.TotalSeconds:0}s (attempt {rateLimitRetries}/{MaxRateLimitRetries}).");
                continue;
            }

            if (!usedProductionFallback && HasStatusCode(result, SandboxNoPreparedDataStatusCode))
            {
                Assert.Warn("Sandbox is not supported for this endpoint. Falling back to api.dataforseo.com.");
                useProductionApi();
                usedProductionFallback = true;
                continue;
            }

            return result;
        }
    }

    private static bool HasStatusCode(object response, int statusCode)
    {
        return GetStatusCodes(response).Contains(statusCode);
    }

    private static IEnumerable<int> GetStatusCodes(object response)
    {
        if (TryGetIntProperty(response, "StatusCode", out var topLevelStatusCode))
            yield return topLevelStatusCode;

        var tasks = response?.GetType().GetProperty("Tasks")?.GetValue(response) as IEnumerable;
        if (tasks == null)
            yield break;

        foreach (var task in tasks)
        {
            if (TryGetIntProperty(task, "StatusCode", out var taskStatusCode))
                yield return taskStatusCode;
        }
    }

    private static bool TryGetIntProperty(object obj, string propertyName, out int value)
    {
        value = 0;
        if (obj == null)
            return false;

        var raw = obj.GetType().GetProperty(propertyName)?.GetValue(obj);
        if (raw == null)
            return false;

        try
        {
            value = Convert.ToInt32(raw);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitIfRateLimitedAsync()
    {
        TimeSpan delay;
        lock (RateLimitSync)
        {
            delay = _rateLimitResumeAtUtc - DateTime.UtcNow;
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay);
    }

    private static void MarkRateLimited()
    {
        lock (RateLimitSync)
        {
            var resumeAt = DateTime.UtcNow.Add(RateLimitDelay);
            if (resumeAt > _rateLimitResumeAtUtc)
                _rateLimitResumeAtUtc = resumeAt;
        }
    }

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