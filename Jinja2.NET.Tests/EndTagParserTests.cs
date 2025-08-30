using FluentAssertions;
using Jinja2.NET.Models;
using Jinja2.NET.Parsers;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class EndTagParserTests
{
    private readonly ITestOutputHelper _output;

    public EndTagParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_WithEmptyEndIf_Should_ConsumeAndReturnNull()
    {
        // Arrange
        var template = "{% endif %}";
        var (tokens, _) = CreateTokenIterator(template);
        var cut = CreateClassUnderTest();
        tokens.Consume(ETokenType.BlockStart);

        // Act
        var node = cut.Parse(tokens, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

        // Assert
        node.Should().BeNull();
        tokens.IsAtEnd().Should().BeTrue();
    }

    [Fact]
    public void Parse_WithExtraTokensAfterEndIf_Should_Throw()
    {
        // Arrange
        var template = "{% endif extra %}";
        var (tokenIterator, tokens) = CreateTokenIterator(template);
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var cut = CreateClassUnderTest();
        tokenIterator.Consume(ETokenType.BlockStart);

        // Act
        Action act = () => cut.Parse(tokenIterator, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Expected BlockEnd at*");
    }

    //[Fact]
    //public void Parse_WithIncorrectEndTag_Should_Throw()
    //{
    //    // Arrange
    //    var template = "{% endfor %}";
    //    var (tokens, _) = CreateTokenIterator(template);
    //    var cut = CreateClassUnderTest();
    //    tokens.Consume(ETokenType.BlockStart);

    //    // Act
    //    Action act = () => cut.Parse(tokens, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

    //    // Assert
    //    act.Should().Throw<InvalidOperationException>()
    //        .WithMessage($"*Expected end tag '{TemplateConstants.BlockNames.EndIf}'*");
    //}

    

    [Fact]
    public void Parse_WithMissingIdentifier_Should_Throw()
    {
        // Arrange
        var template = "{% %}";
        var (tokens, _) = CreateTokenIterator(template);
        var cut = CreateClassUnderTest();
        tokens.Consume(ETokenType.BlockStart);

        // Act
        Action act = () => cut.Parse(tokens, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Expected identifier*");
    }

    [Fact]
    public void Parse_WithTrailingWhitespace_Should_ConsumeAndReturnNull()
    {
        // Arrange
        var template = "{% endif %} \n \t";
        var (tokens, _) = CreateTokenIterator(template);
        var cut = CreateClassUnderTest();
        tokens.Consume(ETokenType.BlockStart);

        // Act
        var node = cut.Parse(tokens, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

        // Assert
        node.Should().BeNull();
        tokens.Peek().Type.Should().Be(ETokenType.Text);
        tokens.Peek().Value.Should().Be(" \n \t");
    }

    [Fact]
    public void Parse_WithValidEndIf_Should_ConsumeAndReturnNull()
    {
        // Arrange
        var template = "{% endif %}text_after";
        var (tokens, _) = CreateTokenIterator(template);
        var cut = CreateClassUnderTest();
        tokens.Consume(ETokenType.BlockStart);

        // Act
        var node = cut.Parse(tokens, null, null, null, new SourceLocation(1, 1, 0), ETokenType.BlockStart);

        // Assert
        node.Should().BeNull();
        tokens.Peek().Type.Should().Be(ETokenType.Text);
        tokens.Peek().Value.Should().Be("text_after");
    }

    [Fact]
    public void Parse_WithWhitespaceBeforeEndIf_Should_ConsumeAndReturnNull()
    {
        // Arrange
        var template = " \n \t{% endif %}text_after";
        var (tokens, _) = CreateTokenIterator(template);
        var cut = CreateClassUnderTest();
        tokens.Consume(ETokenType.Text); // Consume whitespace
        tokens.Consume(ETokenType.BlockStart);

        // Act
        var node = cut.Parse(tokens, null, null, null, new SourceLocation(2, 1, 2), ETokenType.BlockStart);

        // Assert
        node.Should().BeNull();
        tokens.Peek().Type.Should().Be(ETokenType.Text);
        tokens.Peek().Value.Should().Be("text_after");
    }

    private EndTagParser CreateClassUnderTest()
    {
        return new EndTagParser(TemplateConstants.BlockNames.EndIf);
    }

    private (TokenIterator, List<Token>) CreateTokenIterator(string template)
    {
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        return (new TokenIterator(tokens), tokens);
    }
}