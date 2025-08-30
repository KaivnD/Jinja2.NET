using FluentAssertions;
using Jinja2.NET.Nodes;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class IfEdgeCaseIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IfEdgeCaseIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_IfElseWithInvalidEndTag_Should_Throw()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content{% endfor %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        Action act = () => mainParser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>()
            .WithMessage("*Expected 'endif' block*")
            .And.Invoking(e =>
                _output.WriteLine($"Exception: {e.Message}\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
    }

    [Fact]
    public void Parse_IfElseWithNestedIfBlock_Should_ReturnElseBlockWithNested()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}{% if z > 0 %}nested{% endif %}{% endif %}";
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

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Arguments.Should().BeEmpty();

        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be(TemplateConstants.BlockNames.If)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse nested if block\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
    }

    [Fact]
    public void Parse_IfElseWithUnclosedBlock_Should_Throw()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        Action act = () => mainParser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>()
            .WithMessage("*Unclosed 'if' block*")
            .And.Invoking(e =>
                _output.WriteLine($"Exception: {e.Message}\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
    }

    [Fact]
    public void Parse_IfWithMissingCondition_Should_Throw()
    {
        // Arrange
        var template = "{% if %}text{% endif %}";
        var mainParser = new MainParser();
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        Action act = () => mainParser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>()
            .WithMessage("*Parsing failed*")
            .And.Invoking(e =>
                _output.WriteLine($"Exception: {e.Message}\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));
    }

    [Fact]
    public void Parse_IfWithNestedIfElif_Should_ReturnNestedStructure()
    {
        // Arrange
        var template = "{% if x %}{% if y %}a{% elif z %}b{% endif %}{% endif %}";
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

        ifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be(TemplateConstants.BlockNames.If)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse nested if block\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        var nestedIf = (BlockNode)ifNode.Children[0];
        nestedIf.Children.Should().HaveCount(2)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Incorrect nestedIf.Children count\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        nestedIf.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("a");

        var elifNode = nestedIf.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifNode.Name.Should().Be(TemplateConstants.BlockNames.Elif);
        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("b");
    }

    [Fact]
    public void Parse_IfWithNestedIfInElif_Should_ReturnNestedStructure()
    {
        // Arrange
        var template = "{% if x %}foo{% elif y == 1 %}{% if z > 0 %}nested{% endif %}{% endif %}";
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

        elifNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be(TemplateConstants.BlockNames.If)
            .And.Invoking(_ =>
                _output.WriteLine(
                    $"Exception: Failed to parse nested if block\n{TemplateDebugger.DebugTokens("Tokens:", tokens)}"));

        var nestedIf = (BlockNode)elifNode.Children[0];
        nestedIf.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("nested");
    }
}