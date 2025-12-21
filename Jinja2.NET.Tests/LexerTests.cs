using FluentAssertions;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class LexerTests
{
    private readonly ITestOutputHelper _output;

    public LexerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Lexer_Should_Emit_Quoted_String_Tokens()
    {
        // Test both single and double quoted strings
        var template = "{% set foo = 'A' %}{% set bar = \"B\" %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Find all string tokens
        var stringTokens = tokens.Where(t => t.Type == ETokenType.String).ToList();

        stringTokens.Should().HaveCount(2);
        stringTokens[0].Value.Should().Be("'A'");
        stringTokens[1].Value.Should().Be("\"B\"");
    }


    [Theory]
    [InlineData("{{ \"\\n\" }}", "\"\\n\"")]
    [InlineData("{{ \"\\u2668\" }}", "\"\\u2668\"")]
    [InlineData("{{ \"\\\"\" }}", "\"\\\"\"")]
    [InlineData("{{ \"a \\\" b\" }}", "\"a \\\" b\"")]
    public void Lexer_Should_Emit_Quoted_String_Tokens_With_Escapes(string input, string expected)
    {
        var lexer = new Lexer(input);
        var tokens = lexer.Tokenize();
        var stringToken = tokens.First(t => t.Type == ETokenType.String);
        stringToken.Value.Should().Be(expected);
    }

    [Fact]
    public void Lexer_Should_LstripBlocks_After_BlockTag1()
    {
        // Arrange: Template with newlines and spaces after a block tag
        var template = @"{% if true %}
    Hello
{% endif %}";
        var config = new LexerConfig { LstripBlocks = true };
        var lexer = new Lexer(template, config);

        // Act
        var tokens = lexer.Tokenize();

        // Find the text token after the block start
        var textToken = tokens.Find(t => t.Type == ETokenType.Text && t.Value.Contains("Hello"));

        // Assert: The text token should not start with spaces if LstripBlocks is enabled
        textToken.Should().NotBeNull();
        textToken.Value.Should().StartWith("Hello");
    }

    [Fact]
    public void Lexer_Should_LstripBlocks_After_BlockTag2()
    {
        // Arrange: Template with spaces after a block tag
        var template = "{% if true %}   Hello{% endif %}";
        var config = new LexerConfig { LstripBlocks = true };
        var lexer = new Lexer(template, config);

        // Act
        var tokens = lexer.Tokenize();

        // Assert: The text token after the block should not start with spaces
        var textToken = tokens.FirstOrDefault(t => t.Type == ETokenType.Text && t.Value.Contains("Hello"));
        textToken.Should().NotBeNull();
        textToken.Value.Should().StartWith("Hello");
    }

    [Fact]
    public void Lexer_Should_Not_Emit_Text_Tokens_For_Whitespace_Inside_Block_Tags()
    {
        var template = "Hello {%- if true -%} World!";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Find tokens between BlockStart and BlockEnd
        var blockStartIndex = tokens.FindIndex(t => t.Type == ETokenType.BlockStart);
        var blockEndIndex = tokens.FindIndex(blockStartIndex + 1, t => t.Type == ETokenType.BlockEnd);

        // All tokens between BlockStart and BlockEnd should NOT be Text
        for (var i = blockStartIndex + 1; i < blockEndIndex; i++)
            tokens[i].Type.Should().NotBe(ETokenType.Text,
                $"No Text tokens should be inside block tags, but found '{tokens[i].Value}'");
    }

    [Fact]
    public void Lexer_Should_Preserve_Whitespace_Inside_Block()
    {
        var template = "Hello    {%- if true -%} World {%- endif -%}    !";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Find the TextNode inside the if block
        var worldToken = tokens.FirstOrDefault(t => t.Type == ETokenType.Text && t.Value.Contains("World"));
        Assert.NotNull(worldToken);
        Assert.Equal(" World ", worldToken.Value); // Should preserve the space before and after "World"
    }

    [Theory]
    [InlineData(true, "  {% set foo = 1 %}", ETokenType.Text, "")]
    [InlineData(false, "  {% set foo = 1 %}", ETokenType.Text, "  ")]
    public void Lexer_Should_Respect_LstripBlocks(bool lstrip, string template, ETokenType expectedType,
        string expectedText)
    {
        var config = new LexerConfig { LstripBlocks = lstrip };
        var lexer = new Lexer(template, config);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // The first token is always Text (could be empty if lstrip is true)
        tokens[0].Type.Should().Be(expectedType);
        tokens[0].Value.Should().Be(expectedText);
    }

    [Fact]
    public void Lexer_Should_Throw_On_Unclosed_Tag()
    {
        var lexer = new Lexer("Hello {{ name!");
        Action act = () => lexer.Tokenize();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Unclosed tag*");
    }

    [Fact]
    public void Lexer_Should_Tokenize_Complex_Set_Statement()
    {
        var template =
            @"{% set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>' + (message['content'] | trim) + '<|eot_id|>' %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Check for key tokens in order
        tokens.Should().Contain(t => t.Type == ETokenType.BlockStart && t.Value == "{%");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "set");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "content");
        tokens.Should().Contain(t => t.Type == ETokenType.Equals && t.Value == "=");
        tokens.Should().Contain(t => t.Type == ETokenType.Plus && t.Value == "+");
        tokens.Should().Contain(t => t.Type == ETokenType.Pipe && t.Value == "|");
        tokens.Should().Contain(t => t.Type == ETokenType.BlockEnd && t.Value == "%}");
    }

    [Fact]
    public void Lexer_Should_Tokenize_For_Loop_With_Newlines()
    {
        var template = @"{% for item in items %}- {{ item }}
{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Find the tokens for the loop body
        // Expect: BlockStart, Identifier(for), Identifier(item), Identifier(in), Identifier(items), BlockEnd,
        //         Text("- "), VariableStart, Identifier(item), VariableEnd, Text("\r\n" or "\n"),
        //         BlockStart, Identifier(endfor), BlockEnd

        tokens.Should().Contain(t => t.Type == ETokenType.BlockStart && t.Value == "{%");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "for");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "item");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "in");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "items");
        tokens.Should().Contain(t => t.Type == ETokenType.BlockEnd && t.Value == "%}");

        tokens.Should().Contain(t => t.Type == ETokenType.Text && t.Value == "- ");
        tokens.Should().Contain(t => t.Type == ETokenType.VariableStart && t.Value == "{{");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "item");
        tokens.Should().Contain(t => t.Type == ETokenType.VariableEnd && t.Value == "}}");
        tokens.Should().Contain(t =>
            t.Type == ETokenType.Text && (t.Value == "\n" || t.Value == "\r\n"));

        tokens.Should().Contain(t => t.Type == ETokenType.BlockStart && t.Value == "{%");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "endfor");
        tokens.Should().Contain(t => t.Type == ETokenType.BlockEnd && t.Value == "%}");
    }

    [Fact]
    public void Lexer_Should_Tokenize_Indented_Complex_Blocks_With_Whitespace()
    {
        var template = @"
{% set foo = 1 %}
{% for i in [1,2] %}
    {% if i == 1 %}
        One
    {% else %}
        Not one
    {% endif %}
{% endfor %}
";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        // Check that block starts and ends are present in the correct order
        tokens.Should().Contain(t => t.Type == ETokenType.BlockStart && t.Value == "{%");
        tokens.Should().Contain(t => t.Type == ETokenType.BlockEnd && t.Value == "%}");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "set");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "for");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "if");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "else");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "endif");
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "endfor");

        // Check that whitespace and newlines are tokenized as Text
        tokens.Should().Contain(t => t.Type == ETokenType.Text && t.Value.Contains("\n"));

        // Optionally, print tokens for debugging
        foreach (var token in tokens)
        {
            _output.WriteLine($"{token.Type}: '{token.Value.Replace("\n", "\\n")}'");
        }
    }

    [Theory]
    [MemberData(nameof(RawBlockNewlineTestData))]
    public void Lexer_Should_Tokenize_Raw_Blocks_With_Newlines(string template, string[] expectedTextTokens)
    {
        // Arrange
        var lexer = new Lexer(template);

        // Act
        var tokens = lexer.Tokenize();

        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Tokens: ", tokens));
        var textTokens = tokens.Where(t => t.Type == ETokenType.Text).Select(t => t.Value).ToArray();

        // Assert
        textTokens.Should().ContainInOrder(expectedTextTokens);
        textTokens.Length.Should().Be(expectedTextTokens.Length);
    }


    [Theory]
    [InlineData("Hello {{ name -}}!", ETokenType.VariableEnd, "-}}", false, true)]
    [InlineData("{% if true -%}yes{% endif %}", ETokenType.BlockEnd, "-%}", false, true)]
    [InlineData("{# comment -#}", ETokenType.CommentEnd, "-#}", false, true)]
    public void Lexer_Should_Tokenize_RightDash_Whitespace_Control(
        string template,
        ETokenType expectedEndType,
        string expectedEndValue,
        bool expectedTrimLeft,
        bool expectedTrimRight)
    {
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        var endToken = tokens.First(t => t.Type == expectedEndType && t.Value == expectedEndValue);

        endToken.TrimLeft.Should().Be(expectedTrimLeft);
        endToken.TrimRight.Should().Be(expectedTrimRight);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Set_Block_With_Leading_Whitespace()
    {
        var template = "\n{% set foo = 1 %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();

        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var i = 0;
        tokens[i].Type.Should().Be(ETokenType.Text);
        tokens[i++].Value.Should().Be("\n");

        tokens[i].Type.Should().Be(ETokenType.BlockStart);
        tokens[i++].Value.Should().Be("{%");

        i = SkipWhitespace(tokens, i);
        tokens[i].Type.Should().Be(ETokenType.Identifier);
        tokens[i++].Value.Should().Be("set");

        i = SkipWhitespace(tokens, i);
        tokens[i].Type.Should().Be(ETokenType.Identifier);
        tokens[i++].Value.Should().Be("foo");

        i = SkipWhitespace(tokens, i);
        tokens[i].Type.Should().Be(ETokenType.Equals);
        tokens[i++].Value.Should().Be("=");

        i = SkipWhitespace(tokens, i);
        tokens[i].Type.Should().Be(ETokenType.Number);
        tokens[i++].Value.Should().Be("1");
    }

    [Fact]
    public void Lexer_Should_Tokenize_Text_With_Newlines()
    {
        var lexer = new Lexer("Hello\n{{ name }}\nWorld!");
        var tokens = lexer.Tokenize();

        tokens.Should().Contain(t => t.Type == ETokenType.Text && t.Value == "Hello\n");
        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableStart);
        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableEnd);
        tokens.Should().Contain(t => t.Type == ETokenType.Identifier && t.Value == "name");
        tokens.Should().Contain(t => t.Type == ETokenType.Text && t.Value == "\nWorld!");
    }

    [Fact]
    public void Lexer_Should_Tokenize_Variable_And_Text()
    {
        var lexer = new Lexer("Hello {{ name }}!");
        var tokens = lexer.Tokenize();

        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableStart);
        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableEnd);
        tokens.Should().Contain(t => t.Value == "name" && t.Type == ETokenType.Identifier);
        tokens.Should().Contain(t => t.Value == "Hello " && t.Type == ETokenType.Text);
        tokens.Should().Contain(t => t.Value == "!" && t.Type == ETokenType.Text);
    }

    [Fact]
    public void Lexer_Should_Tokenize_Variable_And_Text_With_Newline()
    {
        var lexer = new Lexer(
            @"Hello {{ name }}!
");
        var tokens = lexer.Tokenize();

        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableStart);
        tokens.Should().ContainSingle(t => t.Type == ETokenType.VariableEnd);
        tokens.Should().Contain(t => t.Value == "name" && t.Type == ETokenType.Identifier);
        tokens.Should().Contain(t => t.Value == "Hello " && t.Type == ETokenType.Text);
        tokens.Should().Contain(t =>
            (t.Value == "!\n" || t.Value == "!\r\n") && t.Type == ETokenType.Text);
    }


    [Theory]
    [InlineData("A", 1, 1, ETokenType.Text, "A")]
    [InlineData("A\nB", 1, 1, ETokenType.Text, "A\nB")]
    [InlineData("A\r\nB", 1, 1, ETokenType.Text, "A\nB")]
    [InlineData("A\n  {%- if true -%}\nB", 2, 3, ETokenType.BlockStart, "{%-")]
    [InlineData("A\n  {%- if true -%}\nB", 2, 7, ETokenType.Identifier, "if")]
    [InlineData("A\n  {%- if true -%}\nB", 2, 10, ETokenType.Identifier, "true")]
    [InlineData("A\n  {%- if true -%}\nB", 2, 15, ETokenType.BlockEnd, "-%}")]
    [InlineData("A\n  {%- if true -%}\nB", 3, 1, ETokenType.Text, "\nB")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 3, ETokenType.BlockStart, "{%-")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 7, ETokenType.Identifier, "set")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 11, ETokenType.Identifier, "foo")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 15, ETokenType.Equals, "=")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 17, ETokenType.Number, "1")]
    [InlineData("  {%- set foo = 1 %}\n", 1, 19, ETokenType.BlockEnd, "%}")]
    [InlineData("A\nB\nC", 1, 1, ETokenType.Text, "A\nB\nC")]
    [InlineData("A\r\nB\nC", 1, 1, ETokenType.Text, "A\nB\nC")]
    public void Lexer_Should_Track_Line_And_Column_Correctly(
        string template, int expectedLine, int expectedColumn, ETokenType expectedType, string expectedValue)
    {
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.GetExtendedTokensInfo(tokens, "Lexer Tokens:"));

        var token = tokens.FirstOrDefault(t => t.Type == expectedType && t.Value == expectedValue);
        token.Should().NotBeNull($"Token {expectedType} '{expectedValue}' should exist");
        token.Line.Should().Be(expectedLine, $"Token '{expectedValue}' should be on line {expectedLine}");
        token.Column.Should().Be(expectedColumn, $"Token '{expectedValue}' should be at column {expectedColumn}");
    }

    [Fact]
    public void Lexer_Validate_Should_Return_No_Error_On_Valid_Template()
    {
        var template = "Hello {{ name }}! {% if true %}OK{% endif %} {# comment #}";
        var lexer = new Lexer(template);

        var errors = lexer.Validate();

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{{", "Unclosed tag")]
    [InlineData("{%", "Unclosed tag")]
    [InlineData("{#", "Unclosed tag")]
    public void Lexer_Validate_Should_Return_Syntax_Error_On_Only_Opening_Delimiter(string template,
        string expectedError)
    {
        var lexer = new Lexer(template);

        var errors = lexer.Validate();

        errors.Should().NotBeEmpty();
        errors[0].Should().Contain(expectedError);
        errors[0].Should().Contain("line");
        errors[0].Should().Contain("column");
    }

    [Theory]
    [InlineData("Hello {% if true ")]
    [InlineData("Hello {# comment ")]
    public void Lexer_Validate_Should_Return_Syntax_Error_On_Unclosed_Tag(string template)
    {
        var lexer = new Lexer(template);

        var errors = lexer.Validate();

        errors.Should().NotBeEmpty();
        errors[0].Should().Contain("Unclosed tag");
        errors[0].Should().Contain("line");
        errors[0].Should().Contain("column");
    }

    public static IEnumerable<object[]> RawBlockNewlineTestData()
    {
        yield return new object[]
        {
            // template
            "{% raw %}\nfoo\n{{ bar }}\n{% endraw %}",
            // expected text tokens (in order)
            new[] { "\nfoo\n{{ bar }}\n" }
        };
        yield return new object[]
        {
            // template (not raw)
            "foo\n{{ bar }}\n",
            // expected text tokens (in order, assuming bar is not replaced in lexer)
            new[] { "foo\n", "\n" }
        };
    }

    [Fact]
    public void Should_Tokenize_Whitespace_Control_Dashes_Correctly()
    {
        // Arrange: Template with all combinations of whitespace control dashes
        const string template = @"
A{{- var1 -}}B
C{{- var2}}D
E{{var3 -}}F
G{% -%}H
I{%- if true -%}J{%- endif -%}K
L{#- comment -#}M
";

        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Tokens: ", tokens));

        // Act & Assert: Check the tokens for correct TrimLeft/TrimRight flags
        // Find tokens by their values for clarity
        var var1Start = tokens.First(t => t.Value == "{{-");
        var var1End = tokens.First(t => t.Value == "-}}");
        var var2Start = tokens.First(t => t.Value == "{{-");
        var var2End = tokens.First(t => t.Value == "}}");
        var var3Start = tokens.First(t => t.Value == "{{");
        var var3End = tokens.First(t => t.Value == "-}}");
        var blockStart = tokens.First(t => t.Value == "{%-");
        var blockEnd = tokens.First(t => t.Value == "-%}");
        var commentStart = tokens.First(t => t.Value == "{#-");
        var commentEnd = tokens.First(t => t.Value == "-#}");

        // {{- var1 -}}
        var1Start.TrimLeft.Should().BeTrue();
        var1End.TrimRight.Should().BeTrue();

        // {{- var2}}
        var2Start.TrimLeft.Should().BeTrue();
        var2End.TrimRight.Should().BeFalse();

        // {{var3 -}}
        var3Start.TrimLeft.Should().BeFalse();
        var3End.TrimRight.Should().BeTrue();

        // {%- ... -%}
        blockStart.TrimLeft.Should().BeTrue();
        blockEnd.TrimRight.Should().BeTrue();

        // {#- ... -#}
        commentStart.TrimLeft.Should().BeTrue();
        commentEnd.TrimRight.Should().BeTrue();
    }

    [Fact]
    public void Tokenize_WithMalformedBlockStart_Should_Throw()
    {
        var template = "{% endif";
        var lexer = new Lexer(template);

        Action act = () => lexer.Tokenize();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unclosed tag*");
    }

    private static int SkipWhitespace(IReadOnlyList<Token> tokens, int i)
    {
        while (i < tokens.Count && tokens[i].Type == ETokenType.Text && string.IsNullOrWhiteSpace(tokens[i].Value))
            i++;
        return i;
    }
}