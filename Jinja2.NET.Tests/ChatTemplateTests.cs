using System.Collections.Generic;
using Jinja2.NET.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;
using Jinja2.NET;
using FluentAssertions;

namespace Jinja2.NET.Tests;

public class ChatTemplateTests
{
    private readonly ITestOutputHelper _output;

    public ChatTemplateTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("default", "_base")]
    [InlineData("default", "blenderbot")]
    [InlineData("default", "blenderbot_small")]
    [InlineData("default", "bloom")]
    [InlineData("default", "gpt_neox")]
    [InlineData("default", "gpt2")]
    [InlineData("default", "llama")]
    [InlineData("default", "whisper")]
    [InlineData("custom", "HuggingFaceH4/zephyr-7b-beta (add_generation_prompt=false)")] // str differs
    [InlineData("custom", "HuggingFaceH4/zephyr-7b-beta (add_generation_prompt=true)")] // str differs
    [InlineData("custom", "HuggingFaceH4/zephyr-7b-gemma-v0.1")]
    [InlineData("custom", "TheBloke/Mistral-7B-Instruct-v0.1-GPTQ")]
    [InlineData("custom", "mistralai/Mixtral-8x7B-Instruct-v0.1")]
    [InlineData("custom", "cognitivecomputations/dolphin-2.5-mixtral-8x7b")]
    [InlineData("custom", "openchat/openchat-3.5-0106")]
    [InlineData("custom", "upstage/SOLAR-10.7B-Instruct-v1.0")]
    [InlineData("custom", "codellama/CodeLlama-70b-Instruct-hf")]
    [InlineData("custom", "Deci/DeciLM-7B-instruct")] // str differs
    [InlineData("custom", "Qwen/Qwen1.5-72B-Chat")]
    [InlineData("custom", "deepseek-ai/deepseek-llm-7b-chat")]
    [InlineData("custom", "h2oai/h2o-danube-1.8b-chat")]
    [InlineData("custom", "internlm/internlm2-chat-7b")]
    [InlineData("custom", "TheBloke/deepseek-coder-33B-instruct-AWQ")] // str differs
    [InlineData("custom", "ericzzz/falcon-rw-1b-chat")]
    [InlineData("custom", "abacusai/Smaug-34B-v0.1")]
    [InlineData("custom", "maywell/Synatra-Mixtral-8x7B")] // empty str
    [InlineData("custom", "deepseek-ai/deepseek-coder-33b-instruct")] // str differs
    [InlineData("custom", "meetkai/functionary-medium-v2.2")] // str differs
    [InlineData("custom", "fireworks-ai/firefunction-v1")]
    [InlineData("custom", "maywell/PiVoT-MoE")]
    [InlineData("custom", "CohereForAI/c4ai-command-r-v01")]
    [InlineData("custom", "CohereForAI/c4ai-command-r-v01 (JSON Schema)")]
    [InlineData("custom", "mistralai/Mistral-7B-Instruct-v0.3 (JSON Schema)")]
    [InlineData("custom", "CISCai/Mistral-7B-Instruct-v0.3-SOTA-GGUF")]
    [InlineData("custom", "NousResearch/Hermes-2-Pro-Llama-3-8B (JSON Schema)")]
    [InlineData("custom", "mistralai/Mistral-Nemo-Instruct-2407")]
    [InlineData("custom", "meta-llama/Llama-3.1-8B-Instruct")]
    [InlineData("custom", "deepseek-ai/DeepSeek-R1")]
    [InlineData("custom", "MadeAgents/Hammer2.1")]
    [InlineData("custom", "Qwen/Qwen2.5-7B-Instruct")]
    [InlineData("custom", "Qwen/Qwen2.5-VL-7B-Instruct")]
    [InlineData("custom", "Qwen/Qwen3-0.6B")]
    [InlineData("custom", "CohereLabs/c4ai-command-a-03-2025")]
    [InlineData("custom", "openbmb/MiniCPM3-4B")]
    [InlineData("custom", "ai21labs/AI21-Jamba-Large-1.6")]
    [InlineData("custom", "meta-llama/Llama-3.2-11B-Vision-Instruct")]
    [InlineData("custom", "meta-llama/Llama-Guard-3-11B-Vision")]
    [InlineData("custom", "HuggingFaceTB/SmolLM3-3B")]
    [InlineData("custom", "CohereLabs/command-a-reasoning-08-2025")]
    public void ChatTemplate_Should_Render_Correctly(string group, string name)
    {
        var config = ChatTemplateTestBuilder.GetTemplateConfig(group, name);
        var template = new Template(config.ChatTemplate);
        var result = template.Render(config.Data);
        _output.WriteLine($"Rendered Output:");
        _output.WriteLine(result);
        _output.WriteLine($"Expected Target:");
        _output.WriteLine(config.Target);
        result.Should().Be(config.Target);
    }
}