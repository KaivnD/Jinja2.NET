using FluentAssertions;
using Jinja2.NET;
using Jinja2.NET.Tests.Helpers;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using System.IO;

namespace Jinja2.NET.Tests;

public class ChatTemplateTests
{
    private readonly ITestOutputHelper _output;

    public ChatTemplateTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory(DisplayName = "")]
    [InlineData("_base")]
    [InlineData("blenderbot")]
    [InlineData("blenderbot_small")]
    [InlineData("bloom")]
    [InlineData("gpt_neox")]
    [InlineData("gpt2")]
    [InlineData("llama")]
    [InlineData("whisper")]
    [InlineData("HuggingFaceH4/zephyr-7b-beta (add_generation_prompt=false)")]
    [InlineData("HuggingFaceH4/zephyr-7b-beta (add_generation_prompt=true)")]
    [InlineData("HuggingFaceH4/zephyr-7b-gemma-v0.1")]
    [InlineData("TheBloke/Mistral-7B-Instruct-v0.1-GPTQ")]
    [InlineData("mistralai/Mixtral-8x7B-Instruct-v0.1")]
    [InlineData("cognitivecomputations/dolphin-2.5-mixtral-8x7b")]
    [InlineData("openchat/openchat-3.5-0106")]
    [InlineData("upstage/SOLAR-10.7B-Instruct-v1.0")]
    [InlineData("codellama/CodeLlama-70b-Instruct-hf")]
    [InlineData("Deci/DeciLM-7B-instruct")]
    [InlineData("Qwen/Qwen1.5-72B-Chat")]
    [InlineData("deepseek-ai/deepseek-llm-7b-chat")]
    [InlineData("h2oai/h2o-danube-1.8b-chat")]
    [InlineData("internlm/internlm2-chat-7b")]
    [InlineData("TheBloke/deepseek-coder-33B-instruct-AWQ")]
    [InlineData("ericzzz/falcon-rw-1b-chat")]
    [InlineData("abacusai/Smaug-34B-v0.1")]
    [InlineData("maywell/Synatra-Mixtral-8x7B")]
    [InlineData("deepseek-ai/deepseek-coder-33b-instruct")]
    [InlineData("meetkai/functionary-medium-v2.2")]
    [InlineData("fireworks-ai/firefunction-v1")] // parsing issue
    [InlineData("maywell/PiVoT-MoE")] // parsing issue
    [InlineData("CohereForAI/c4ai-command-r-v01")] // call method items from dictionary issue
    [InlineData("CohereForAI/c4ai-command-r-v01 (JSON Schema)")] // macro feature
    [InlineData("mistralai/Mistral-7B-Instruct-v0.3 (JSON Schema)")] // parsing issue
    [InlineData("CISCai/Mistral-7B-Instruct-v0.3-SOTA-GGUF")]
    [InlineData("NousResearch/Hermes-2-Pro-Llama-3-8B (JSON Schema)")] // macro feature
    [InlineData("mistralai/Mistral-Nemo-Instruct-2407")]  // parsing issue
    [InlineData("meta-llama/Llama-3.1-8B-Instruct")] // parsing issue
    [InlineData("deepseek-ai/DeepSeek-R1")]
    [InlineData("MadeAgents/Hammer2.1")] // parsing issue
    [InlineData("Qwen/Qwen2.5-7B-Instruct")] // unknown
    [InlineData("Qwen/Qwen2.5-VL-7B-Instruct")] // binary operation issue
    [InlineData("Qwen/Qwen3-0.6B")]
    [InlineData("CohereLabs/c4ai-command-a-03-2025")]  // parsing issue
    [InlineData("openbmb/MiniCPM3-4B")] // parsing issue
    [InlineData("ai21labs/AI21-Jamba-Large-1.6")]  // parsing issue
    [InlineData("meta-llama/Llama-3.2-11B-Vision-Instruct")] // parsing issue
    [InlineData("meta-llama/Llama-Guard-3-11B-Vision")]  // parsing issue
    [InlineData("HuggingFaceTB/SmolLM3-3B")] // parsing issue
    [InlineData("CohereLabs/command-a-reasoning-08-2025")]  // parsing issue
    public void ChatTemplate_Should_Render_Correctly(string m)
    {
        var config = ChatTemplateTestBuilder.GetTemplateConfig(m);
        var template = new Template(config.ChatTemplate);
        var result = template.Render(config.Data);

        var saveResultPath = Path.Combine(config.Location, "result.txt");
        File.WriteAllText(saveResultPath, result); // for debugging
        result.Should().Be(config.Target);
    }
}