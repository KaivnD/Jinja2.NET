/**
 *  huggingface jinja C# implement e2e tests for chat template rendering
 * original file: https://github.com/huggingface/huggingface.js/blob/main/packages/jinja/test/e2e.test.js
 */

using System.Collections.ObjectModel;
using System.Text.Json;

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
    public static readonly List<Dictionary<string, object>> ExampleChat;
    public static readonly List<Dictionary<string, object>> ExampleChatWithSystem;
    public static readonly List<Dictionary<string, object>> ExampleFunctionCalling;
    public static readonly List<Dictionary<string, object>> ExampleFunctionSpec;
    public static readonly List<Dictionary<string, object>> ExampleFunctionCallingWithSystem;
    public static readonly Dictionary<string, Dictionary<string, object>> ExampleToolJsonSchemas;
    public static readonly List<Dictionary<string, object>> ExampleListOfTools;
    static ChatTemplateTestBuilder()
    {
        ExampleChat = BuildExampleChat();
        ExampleChatWithSystem = BuildExampleChatWithSystem();
        ExampleFunctionCalling = BuildExampleFunctionCalling();
        ExampleFunctionSpec = BuildExampleFunctionSpec();
        ExampleFunctionCallingWithSystem = BuildExampleFunctionCallingWithSystem();
        ExampleToolJsonSchemas = BuildExampleToolJsonSchemas();
        ExampleListOfTools = BuildExampleListOfTools();
        DefaultTemplates = BuildDefaultTemplates();
    }
    private static List<Dictionary<string, object>> BuildExampleChat()
    {
        return new List<Dictionary<string, object>>
            {
                CreateChatMessage("user", "Hello, how are you?"),
                CreateChatMessage("assistant", "I'm doing great. How can I help you today?"),
                CreateChatMessage("user", "I'd like to show off how chat templating works!")
            };
    }
    private static List<Dictionary<string, object>> BuildExampleChatWithSystem()
    {
        var chatWithSystem = new List<Dictionary<string, object>>
            {
                CreateChatMessage("system", "You are a friendly chatbot who always responds in the style of a pirate")
            };
        chatWithSystem.AddRange(ExampleChat);
        return chatWithSystem;
    }
    private static List<Dictionary<string, object>> BuildExampleFunctionCalling()
    {
        var toolCall = CreateDict(
            "type", "function",
            "function", CreateDict(
                "name", "get_current_weather",
                "arguments", "{\n  \"location\": \"Hanoi\"\n}"
            )
        );
        return new List<Dictionary<string, object>>
            {
                CreateDict(
                    "role", "assistant",
                    "content", null,
                    "tool_calls", new List<Dictionary<string, object>> { toolCall }
                ),
                CreateChatMessage("user", "what's the weather like in Hanoi?")
            };
    }
    private static List<Dictionary<string, object>> BuildExampleFunctionSpec()
    {
        var stockPriceProperties = CreateDict(
            "symbol", CreateFunctionProperty("string", "The stock symbol, e.g. AAPL, GOOG")
        );
        var checkAnagramProperties = CreateDict(
            "word1", CreateFunctionProperty("string", "The first word"),
            "word2", CreateFunctionProperty("string", "The second word")
        );
        return new List<Dictionary<string, object>>
            {
                CreateDict(
                    "name", "get_stock_price",
                    "description", "Get the current stock price",
                    "parameters", CreateFunctionParameters(stockPriceProperties, new List<string> { "symbol" })
                ),
                CreateDict(
                    "name", "check_word_anagram",
                    "description", "Check if two words are anagrams of each other",
                    "parameters", CreateFunctionParameters(checkAnagramProperties, new List<string> { "word1", "word2" })
                )
            };
    }
    private static List<Dictionary<string, object>> BuildExampleFunctionCallingWithSystem()
    {
        var functionSpecJson = JsonSerializer.Serialize(ExampleFunctionSpec, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        return new List<Dictionary<string, object>>
            {
                CreateChatMessage("functions", functionSpecJson),
                CreateChatMessage("system", "You are a helpful assistant with access to functions. Use them if required."),
                CreateChatMessage("user", "Hi, can you tell me the current stock price of AAPL?")
            };
    }
    private static Dictionary<string, Dictionary<string, object>> BuildExampleToolJsonSchemas()
    {
        var weatherProps = CreateDict(
            "location", CreateFunctionProperty("string", "The city and state, e.g. San Francisco, CA"),
            "unit", CreateFunctionProperty("string", "", new List<string> { "celsius", "fahrenheit" })
        );
        var weatherParams = CreateFunctionParameters(weatherProps, new List<string> { "location" });
        var getCurrentWeather = CreateToolFunction("get_current_weather", "Get the current weather in a given location", weatherParams);
        var tempV1Props = CreateDict(
            "location", CreateFunctionProperty("string", "The location to get the temperature for, in the format \"City, Country\"")
        );
        var tempV1Params = CreateFunctionParameters(tempV1Props, new List<string> { "location" });
        var tempV1Return = CreateFunctionReturnConfig("number", "The current temperature at the specified location in the specified units, as a float.");
        var getCurrentTemperatureV1 = CreateToolFunction("get_current_temperature", "Get the current temperature at a location.", tempV1Params, tempV1Return);
        var tempV2Props = CreateDict(
            "location", CreateFunctionProperty("string", "The location to get the temperature for, in the format \"City, Country\""),
            "unit", CreateFunctionProperty("string", "The unit to return the temperature in.", new List<string> { "celsius", "fahrenheit" })
        );
        var tempV2Params = CreateFunctionParameters(tempV2Props, new List<string> { "location", "unit" });
        var tempV2Return = CreateFunctionReturnConfig("number", "The current temperature at the specified location in the specified units, as a float.");
        var getCurrentTemperatureV2 = CreateToolFunction("get_current_temperature", "Get the current temperature at a location.", tempV2Params, tempV2Return);
        var windSpeedProps = CreateDict(
            "location", CreateFunctionProperty("string", "The location to get the temperature for, in the format \"City, Country\"")
        );
        var windSpeedParams = CreateFunctionParameters(windSpeedProps, new List<string> { "location" });
        var windSpeedReturn = CreateFunctionReturnConfig("number", "The current wind speed at the given location in km/h, as a float.");
        var getCurrentWindSpeed = CreateToolFunction("get_current_wind_speed", "Get the current wind speed in km/h at a given location.", windSpeedParams, windSpeedReturn);
        return new Dictionary<string, Dictionary<string, object>>
            {
                { "get_current_weather", getCurrentWeather },
                { "get_current_temperature_v1", getCurrentTemperatureV1 },
                { "get_current_temperature_v2", getCurrentTemperatureV2 },
                { "get_current_wind_speed", getCurrentWindSpeed }
            };
    }
    private static List<Dictionary<string, object>> BuildExampleListOfTools()
    {
        return new List<Dictionary<string, object>>
            {
                ExampleToolJsonSchemas["get_current_temperature_v2"],
                ExampleToolJsonSchemas["get_current_wind_speed"]
            };
    }
    private static ReadOnlyDictionary<string, TemplateConfig> BuildDefaultTemplates()
    {
        var defaultTemplates = new Dictionary<string, TemplateConfig>
            {
                {
                    "_base", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "add_generation_prompt", false,
                            "eos_token", (string)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = "<|im_start|>user\nHello, how are you?<|im_end|>\n<|im_start|>assistant\nI'm doing great. How can I help you today?<|im_end|>\n<|im_start|>user\nI'd like to show off how chat templating works!<|im_end|>\n"
                    }
                },
                {
                    "blenderbot", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{% if message['role'] == 'user' %}{{ ' ' }}{% endif %}{{ message['content'] }}{% if not loop.last %}{{ '  ' }}{% endif %}{% endfor %}{{ eos_token }}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "</s>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = " Hello, how are you?  I'm doing great. How can I help you today?   I'd like to show off how chat templating works!</s>"
                    }
                },
                {
                    "blenderbot_small", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{% if message['role'] == 'user' %}{{ ' ' }}{% endif %}{{ message['content'] }}{% if not loop.last %}{{ '  ' }}{% endif %}{% endfor %}{{ eos_token }}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "</s>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = " Hello, how are you?  I'm doing great. How can I help you today?   I'd like to show off how chat templating works!</s>"
                    }
                },
                {
                    "bloom", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{{ message.content }}{{ eos_token }}{% endfor %}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "</s>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = "Hello, how are you?</s>I'm doing great. How can I help you today?</s>I'd like to show off how chat templating works!</s>"
                    }
                },
                {
                    "gpt_neox", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{{ message.content }}{{ eos_token }}{% endfor %}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "<|endoftext|>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = "Hello, how are you?<|endoftext|>I'm doing great. How can I help you today?<|endoftext|>I'd like to show off how chat templating works!<|endoftext|>"
                    }
                },
                {
                    "gpt2", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{{ message.content }}{{ eos_token }}{% endfor %}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "<|endoftext|>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = "Hello, how are you?<|endoftext|>I'm doing great. How can I help you today?<|endoftext|>I'd like to show off how chat templating works!<|endoftext|>"
                    }
                },
                {
                    "llama", new TemplateConfig
                    {
                        ChatTemplate = "{% if messages[0]['role'] == 'system' %}{% set loop_messages = messages[1:] %}{% set system_message = messages[0]['content'] %}{% elif USE_DEFAULT_PROMPT == true and not '<<SYS>>' in messages[0]['content'] %}{% set loop_messages = messages %}{% set system_message = 'DEFAULT_SYSTEM_MESSAGE' %}{% else %}{% set loop_messages = messages %}{% set system_message = false %}{% endif %}{% for message in loop_messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if loop.index0 == 0 and system_message != false %}{% set content = '<<SYS>>\\n' + system_message + '\\n<</SYS>>\\n\\n' + message['content'] %}{% else %}{% set content = message['content'] %}{% endif %}{% if message['role'] == 'user' %}{{ bos_token + '[INST] ' + content.strip() + ' [/INST]' }}{% elif message['role'] == 'system' %}{{ '<<SYS>>\\n' + content.strip() + '\\n<</SYS>>\\n\\n' }}{% elif message['role'] == 'assistant' %}{{ ' ' + content.strip() + ' ' + eos_token }}{% endif %}{% endfor %}",
                        Data = CreateDict(
                            "messages", ExampleChatWithSystem,
                            "bos_token", "<s>",
                            "eos_token", "</s>",
                            "USE_DEFAULT_PROMPT", true,
                            "add_generation_prompt", (bool?)null
                        ),
                        Target = "<s>[INST] <<SYS>>\nYou are a friendly chatbot who always responds in the style of a pirate\n<</SYS>>\n\nHello, how are you? [/INST] I'm doing great. How can I help you today? </s><s>[INST] I'd like to show off how chat templating works! [/INST]"
                    }
                },
                {
                    "whisper", new TemplateConfig
                    {
                        ChatTemplate = "{% for message in messages %}{{ message.content }}{{ eos_token }}{% endfor %}",
                        Data = CreateDict(
                            "messages", ExampleChat,
                            "eos_token", "<|endoftext|>",
                            "add_generation_prompt", (bool?)null,
                            "bos_token", (string)null,
                            "USE_DEFAULT_PROMPT", (bool?)null
                        ),
                        Target = "Hello, how are you?<|endoftext|>I'm doing great. How can I help you today?<|endoftext|>I'd like to show off how chat templating works!<|endoftext|>"
                    }
                }
            };
        return new Dictionary<string, TemplateConfig>(defaultTemplates).AsReadOnly();
    }
    public static Dictionary<string, object> CreateDict(params object[] keyValuePairs)
    {
        if (keyValuePairs.Length % 2 != 0)
            throw new ArgumentException("键值对参数必须成对传入（key, value交替）");
        var dict = new Dictionary<string, object>();
        for (int i = 0; i < keyValuePairs.Length; i += 2)
        {
            string key = keyValuePairs[i] as string ??
                throw new ArgumentException($"第{i + 1}个参数必须是字符串类型的Key");
            object value = keyValuePairs[i + 1];
            dict.Add(key, value);
        }
        return dict;
    }
    public static Dictionary<string, object> CreateChatMessage(string role, object content)
    {
        return CreateDict("role", role, "content", content);
    }
    public static Dictionary<string, object> CreateFunctionParameters(Dictionary<string, object> properties, List<string> requiredFields)
    {
        return CreateDict(
            "type", "object",
            "properties", properties,
            "required", requiredFields
        );
    }
    public static Dictionary<string, object> CreateFunctionProperty(string type, string description, List<string> enumValues = null)
    {
        var props = new List<object> { "type", type, "description", description };
        if (enumValues != null && enumValues.Any())
        {
            props.Add("enum");
            props.Add(enumValues);
        }
        return CreateDict(props.ToArray());
    }
    public static Dictionary<string, object> CreateToolFunction(string functionName, string description, Dictionary<string, object> parameters, Dictionary<string, object> returnConfig = null)
    {
        var functionProps = new List<object>
            {
                "name", functionName,
                "description", description,
                "parameters", parameters
            };
        if (returnConfig != null)
        {
            functionProps.Add("return");
            functionProps.Add(returnConfig);
        }
        return CreateDict(
            "type", "function",
            "function", CreateDict(functionProps.ToArray())
        );
    }
    public static Dictionary<string, object> CreateFunctionReturnConfig(string type, string description)
    {
        return CreateDict("type", type, "description", description);
    }
}