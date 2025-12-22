/**
 *  huggingface jinja C# implement e2e tests for chat template rendering
 * original file: https://github.com/huggingface/huggingface.js/blob/main/packages/jinja/test/e2e.test.js
 */

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jinja2.NET.Tests.Helpers;

public class TemplateConfig
{
    public string ChatTemplate { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public string Target { get; set; }
}
public static class ChatTemplateTestBuilder
{
    public static readonly ReadOnlyDictionary<string, TemplateConfig> DefaultTemplates;
    public static readonly ReadOnlyDictionary<string, TemplateConfig> CustomTemplates;
    public static TemplateConfig GetTemplateConfig(string name)
    {
        if (DefaultTemplates.TryGetValue(name, out TemplateConfig? value))
        {
            return value;
        }
        else if (CustomTemplates.TryGetValue(name, out TemplateConfig? value1))
        {
            return value1;
        }
        else
        {
            throw new ArgumentException($"Template config not found for name '{name}'");
        }
    }

    static ChatTemplateTestBuilder()
    {
        var location = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "chat_template_e2e_test.json"));
        var jsonString = File.ReadAllText(location);
        var root = JsonNode.Parse(jsonString)!.AsObject();
        var defaultTemplates = new Dictionary<string, TemplateConfig>();
        var customTemplates = new Dictionary<string, TemplateConfig>();

        foreach (var (key, value) in root["default"]!.AsObject())
        {
            defaultTemplates[key] = ParseTemplateConfig(value!.AsObject());
        }

        foreach (var (key, value) in root["custom"]!.AsObject())
        {
            customTemplates[key] = ParseTemplateConfig(value!.AsObject());
        }

        DefaultTemplates = new ReadOnlyDictionary<string, TemplateConfig>(defaultTemplates);
        CustomTemplates = new ReadOnlyDictionary<string, TemplateConfig>(customTemplates);
    }

    static TemplateConfig ParseTemplateConfig(JsonObject templateObj)
    {
        return new TemplateConfig
        {
            ChatTemplate = templateObj["chat_template"]!.GetValue<string>(),
            Data = templateObj["data"]!.AsObject().ToDictionary()!,
            Target = templateObj["target"]!.GetValue<string>()
        };
    }
}


public static class JsonObjectExtensions
{
    public static Dictionary<string, object?> ToDictionary(this JsonObject jsonObject)
    {
        if (jsonObject == null)
            throw new ArgumentNullException(nameof(jsonObject), "待转换的JsonObject不能为null");
        var resultDict = new Dictionary<string, object?>();
        foreach (var (key, jsonNode) in jsonObject)
        {
            resultDict[key] = ConvertJsonNodeToObject(jsonNode);
        }
        return resultDict;
    }
    private static object? ConvertJsonNodeToObject(JsonNode? jsonNode)
    {
        if (jsonNode == null)
            return null;
        if (jsonNode is JsonObject nestedJsonObject)
            return nestedJsonObject.ToDictionary();
        if (jsonNode is JsonArray jsonArray)
        {
            var arrayList = new List<object?>();
            foreach (var arrayItem in jsonArray)
            {
                arrayList.Add(ConvertJsonNodeToObject(arrayItem));
            }
            return arrayList;
        }
        var jsonValue = jsonNode.AsValue();
        if (jsonValue.TryGetValue(out bool boolValue))
            return boolValue;
        if (jsonValue.TryGetValue(out int intValue))
            return intValue;
        if (jsonValue.TryGetValue(out long longValue))
            return longValue;
        if (jsonValue.TryGetValue(out decimal decimalValue))
            return decimalValue;
        if (jsonValue.TryGetValue(out double doubleValue))
            return doubleValue;
        if (jsonValue.TryGetValue(out float floatValue))
            return floatValue;
        if (jsonValue.TryGetValue(out string stringValue))
            return stringValue;
        if (jsonValue.TryGetValue(out DateTime dateTimeValue))
            return dateTimeValue;
        return jsonValue.GetValue<object?>();
    }
}