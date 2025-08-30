using FluentAssertions;
using Jinja2.NET.Nodes;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class IfElseIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public IfElseIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_IfElseWithContent_Should_ReturnIfWithElseBlockAndText()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content{% endif %}";
        var mainParser = new MainParser();

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>();
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2);

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Arguments.Should().BeEmpty();
        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("content");
    }

    [Fact]
    public void Parse_IfElseWithEmptyContent_Should_ReturnIfWithEmptyElseBlock()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}{% endif %}";
        var mainParser = new MainParser();

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>();
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2);

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Arguments.Should().BeEmpty();
        elseNode.Children.Should().BeEmpty();
    }

    [Fact]
    public void Parse_IfElseWithInvalidEndTag_Should_Throw()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content{% endfor %}";
        var mainParser = new MainParser();

        // Act
        Action act = () => mainParser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>()
            .WithMessage("*Expected 'endif' block*");
    }

    [Fact]
    public void Parse_IfElseWithNestedIfBlock_Should_ReturnElseBlockWithNested()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}{% if z > 0 %}nested{% endif %}{% endif %}";
        var mainParser = new MainParser();

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>();
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2);

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Arguments.Should().BeEmpty();

        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be(TemplateConstants.BlockNames.If);
    }

    [Fact]
    public void Parse_IfElseWithTrailingContent_Should_PreserveTrailingTokens()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content{% endif %}trailing";
        var mainParser = new MainParser();

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>();

        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2);

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("content");

        // Trailing content
        templateNode.Children[1].Should().BeOfType<TextNode>().Which.Content.Should().Be("trailing");
    }

    [Fact]
    public void Parse_IfElseWithUnclosedBlock_Should_Throw()
    {
        // Arrange
        var template = "{% if x %}foo{% else %}content";
        var mainParser = new MainParser();

        // Act
        Action act = () => mainParser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>()
            .WithMessage("*Unclosed 'if' block*");
    }

    [Fact]
    public void Parse_IfElseWithWhitespaceOnlyContent_Should_ReturnElseBlockWithText()
    {
        // Arrange
        var template = "{% if x %}foo{% else %} \n \t{% endif %}";
        var mainParser = new MainParser();

        // Act
        var templateNode = mainParser.Parse(template);

        // Assert
        templateNode.Should().BeOfType<TemplateNode>();
        var ifNode = templateNode.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifNode.Name.Should().Be(TemplateConstants.BlockNames.If);

        ifNode.Children.Should().HaveCount(2);

        ifNode.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("foo");

        var elseNode = ifNode.Children[1].Should().BeOfType<BlockNode>().Subject;
        elseNode.Name.Should().Be(TemplateConstants.BlockNames.Else);
        elseNode.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be(" \n \t");
    }
}