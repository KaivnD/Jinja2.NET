using FluentAssertions;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class IntegrationOllamaTests
{
    private readonly ITestOutputHelper _output;

    public IntegrationOllamaTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Should_Render_Ollama_GGUF_Style()
    {
        const string templateString = @"
{
{%- set loop_messages = messages -%}
{%- for message in loop_messages -%}
    {%- set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>' + (message['content'] | trim) + '<|eot_id|>' -%}
    {%- if loop.index0 == 0 -%}
        {%- set content = bos_token + content -%}
    {%- endif -%}
    {{- content }}
{%- endfor -%}
{
    { '<|start_header_id|>assistant<|end_header_id|>' }
}
}";
        var template = new Template(templateString);

        var messages = new[]
        {
            new Dictionary<string, object>
            {
                ["role"] = "user",
                ["content"] = "Hello!"
            },
            new Dictionary<string, object>
            {
                ["role"] = "assistant",
                ["content"] = "Hi there."
            }
        };

        //var context = new
        //{
        //    messages,
        //    bos_token = "<|bos_id|>"
        //};

        // Use a dictionary for context
        var context = new Dictionary<string, object>
        {
            ["messages"] = messages,
            ["bos_token"] = "<|bos_id|>"
        };

        var expected = @"
{<|bos_id|><|start_header_id|>user<|end_header_id|>Hello!<|eot_id|>
<|start_header_id|>assistant<|end_header_id|>Hi there.<|eot_id|>
{
    { '<|start_header_id|>assistant<|end_header_id|>' }
}
}".Replace("\r\n", "\n").TrimEnd();


        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));
        var result = template.Render(context);
        _output.WriteLine($"Actual result: '{result}'");
        _output.WriteLine($"Expected result: '{expected}'");
        result.Should().Be(expected);
        result.Should().Be(expected);
    }
}