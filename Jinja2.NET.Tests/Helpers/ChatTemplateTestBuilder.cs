/**
 *  huggingface jinja C# implement e2e tests for chat template rendering
 * original file: https://github.com/huggingface/huggingface.js/blob/main/packages/jinja/test/e2e.test.js
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Jinja2.NET.Tests.Helpers;

public class TemplateConfig
{
    public string ChatTemplate { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public string Target { get; set; }
    public string Location { get; set; }
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
        // 首先尝试从目录结构加载（Data/chat_template_e2e_test/{default,custom}/{name}）
        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Data", "chat_template_e2e_test"));
        var defaultTemplates = new Dictionary<string, TemplateConfig>();
        var customTemplates = new Dictionary<string, TemplateConfig>();

        if (Directory.Exists(baseDir))
        {
            var defaultDir = Path.Combine(baseDir, "default");
            var customDir = Path.Combine(baseDir, "custom");

            CollectTemplatesRecursive(defaultDir, defaultDir, defaultTemplates);
            CollectTemplatesRecursive(customDir, customDir, customTemplates);

            DefaultTemplates = new ReadOnlyDictionary<string, TemplateConfig>(defaultTemplates);
            CustomTemplates = new ReadOnlyDictionary<string, TemplateConfig>(customTemplates);
            return;
        }
    }

    static TemplateConfig ParseTemplateConfigFromDirectory(string dir)
    {
        var chatTemplatePathJinja = Path.Combine(dir, "chat_template.jinja");
        var chatTemplatePathTxt = Path.Combine(dir, "chat_template.txt");
        var chatTemplatePath = File.Exists(chatTemplatePathJinja) ? chatTemplatePathJinja : chatTemplatePathTxt;
        var dataPath = Path.Combine(dir, "data.json");
        var targetPath = Path.Combine(dir, "target.txt");

        if (!File.Exists(chatTemplatePath))
            throw new FileNotFoundException($"Missing chat_template in {dir}");
        if (!File.Exists(dataPath))
            throw new FileNotFoundException($"Missing data.json in {dir}");
        if (!File.Exists(targetPath))
            throw new FileNotFoundException($"Missing target.txt in {dir}");

        var chatTemplate = File.ReadAllText(chatTemplatePath);
        var dataJson = File.ReadAllText(dataPath);
        var target = File.ReadAllText(targetPath);

        var dataNode = JsonNode.Parse(dataJson)!.AsObject();

        return new TemplateConfig
        {
            ChatTemplate = chatTemplate,
            Data = dataNode.ToDictionary()!,
            Target = target,
            Location = dir
        };
    }

    static void CollectTemplatesRecursive(string baseDir, string currentDir, Dictionary<string, TemplateConfig> outDict)
    {
        // if currentDir contains a template, register it
        var hasData = File.Exists(Path.Combine(currentDir, "data.json"));
        var hasTarget = File.Exists(Path.Combine(currentDir, "target.txt"));
        var hasTemplate = File.Exists(Path.Combine(currentDir, "chat_template.jinja"));
        if (hasData && hasTarget && hasTemplate)
        {
            var rel = Path.GetRelativePath(baseDir, currentDir).Replace(Path.DirectorySeparatorChar, '/');
            if (string.IsNullOrEmpty(rel) || rel == ".") rel = Path.GetFileName(currentDir);
            outDict[rel] = ParseTemplateConfigFromDirectory(currentDir);
            return; // don't descend further into this test case dir
        }

        // otherwise, recurse into subdirectories
        foreach (var sub in Directory.EnumerateDirectories(currentDir))
        {
            CollectTemplatesRecursive(baseDir, sub, outDict);
        }
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