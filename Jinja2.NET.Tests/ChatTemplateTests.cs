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
    [InlineData("_base")]
    [InlineData("blenderbot")]
    [InlineData("blenderbot_small")]
    [InlineData("bloom")]
    [InlineData("gpt_neox")]
    [InlineData("gpt2")]
    [InlineData("llama")]
    [InlineData("whisper")]
    public void DefaultsTemplate_Should_Render_Correctly(string name)
    {
        var config = ChatTemplateTestBuilder.DefaultTemplates[name];
        _output.WriteLine($"Template Content:");
        _output.WriteLine(config.ChatTemplate);
        var template = new Template(config.ChatTemplate);
        var result = template.Render(config.Data);
        _output.WriteLine($"Rendered Output:");
        _output.WriteLine(result);
        Assert.Equal(config.Target, result);
    }
}