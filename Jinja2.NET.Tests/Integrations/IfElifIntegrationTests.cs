using FluentAssertions;
using Jinja2.NET.Nodes;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class IfElifIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IfElifIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_IfElifElseEndIf_Should_ReturnIfWithElifAndElse()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 %}bar{% else %}baz{% endif %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>()
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse template\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);
        ifNode.Arguments.Should().HaveCount(1);

        ifNode.Children.Should().HaveCount(3)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect ifNode.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elifNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Arguments.Should().HaveCount(1);
        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("bar");

        var elseNode = ifNode.Children[2].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("baz");
    }

    [Fact]
    public void Parse_IfElifEndIf_Should_ReturnIfWithElifChild()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 %}bar{% endif %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>()
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse template\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);
        ifNode.Arguments.Should().HaveCount(1);

        ifNode.Children.Should().HaveCount(2)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect ifNode.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elifNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Arguments.Should().HaveCount(1);
        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("bar");
    }

    [Fact]
    public void Parse_IfWithComplexElifCondition_Should_ReturnIfWithElif()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 or z > 0 %}other{% endif %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>()
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse template\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect ifNode.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elifNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Arguments.Should().HaveCount(1);
        elifNode.Arguments[0].Should().BeOfType<BinaryExpressionNode>();
        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("other");
    }

    [Fact]
    public void Parse_IfWithElifAndTrailingContent_Should_PreserveTrailingTokens()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 %}other{% endif %}trailing";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>()
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse template\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect ifNode.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elifNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("other");

        templateNode.Children[1].Should().BeOfType<TextNode>().Which.Content.Should().Be("trailing");
    }

    [Fact]
    public void Parse_IfWithEmptyElifContent_Should_ReturnEmptyElifBlock()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 %}{% endif %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>()
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse template\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect ifNode.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elifNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Arguments.Should().HaveCount(1);
        elifNode.Children.Should().BeEmpty();
    }
}