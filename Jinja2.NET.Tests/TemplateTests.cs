using FluentAssertions;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class TemplateTests
{
    private readonly ITestOutputHelper _output;

    public TemplateTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Template_Should_Render_Attribute_Access()
    {
        const string templateSource = "User: {{ user.name }} ({{ user.email }})";

        //var lexer = new Lexer(templateSource);
        //var tokens = lexer.Tokenize();
        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateSource);


        var result = template.Render(new
        {
            user = new { name = "Bob", email = "bob@example.com" }
        });
        result.Should().Be("User: Bob (bob@example.com)");
    }

    [Fact]
    public void Template_Should_Render_Basic_Variables()
    {
        var template = new Template("Hello {{ name }}! You are {{ age }} years old.");
        var result = template.Render(new { name = "Alice", age = 30 });
        result.Should().Be("Hello Alice! You are 30 years old.");
    }

    [Fact]
    public void Template_Should_Render_Chained_Filters()
    {
        var templateSource = "{{ text | lower | capitalize }}";
        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateSource);
        var result = template.Render(new { text = "HELLO WORLD" });
        result.Should().Be("Hello world");
    }

    [Fact]
    public void Template_Should_Render_Complex_Nested_Substitution()
    {
        const string templateSource = @"
User: {{ user.name }} ({{ user.email }})
Roles: {{ user.roles | join(', ') }}
First Role: {{ user.roles[0] }}
Profile: {{ user.profile['bio'] | upper }}
Missing: {{ user.missing_property | default('N/A') }}
";

        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateSource);

        var context = new
        {
            user = new
            {
                name = "Alice",
                email = "alice@example.com",
                roles = new[] { "admin", "editor" },
                profile = new Dictionary<string, object>
                {
                    ["bio"] = "loves coding"
                }
            }
        };

        var expected = @"
User: Alice (alice@example.com)
Roles: admin, editor
First Role: admin
Profile: LOVES CODING
Missing: N/A
".Replace("\r\n", "\n").Trim();

        var result = template.Render(context).Replace("\r\n", "\n").Trim();
        result.Should().Be(expected);
    }

    [Fact]
    public void Template_Should_Render_Dictionary_Context()
    {
        var template = new Template("Items: {{ items | join(', ') }}");
        var result = template.Render(new Dictionary<string, object>
        {
            ["items"] = new[] { "apple", "banana", "cherry" }
        });
        result.Should().Be("Items: apple, banana, cherry");
    }

    [Fact]
    public void Template_Should_Render_Loop_Index0()
    {
        const string templateSource = "{% for item in items %}{{ loop.index0 }}:{{ item }}\n{% endfor %}";

        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        var template = new Template(templateSource);
        var result = template.Render(new { items = new[] { "x", "y" } });
        result.Replace("\r\n", "\n").Trim().Should().Be("0:x\n1:y");
    }

    [Fact]
    public void Template_Should_Render_Set_And_Concat()
    {
        const string templateSource = @"
{% set foo = 'A' + 'B' %}
{{ foo }}
";
        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateSource);
        var result = template.Render(new { });
        result.Trim().Should().Be("AB");
    }

    [Fact]
    public void Template_Should_Render_Set_Inside_Loop_Compact_NoLstrip()
    {
        // Note: No leading/trailing newlines or spaces in the template string!
        const string templateString = "{% for item in items %}{% set foo = 'A' + item %}{{ foo }}\n{% endfor %}";
        var template = new Template(templateString); // No LstripBlocks
        var result = template.Render(new { items = new[] { "X", "Y" } });
        result.Should().Be("AX\nAY\n");
    }

    /*
\n                              // <-- first newline from opening @"
{% for item in items %}
\n    {% set foo = ... %}       // <-- newline after for
\n    {{ foo }}                 // <-- newline after set
\n{% endfor %}                  // <-- newline after variable
\n                              // <-- newline before closing quote
    *
     */
    [Theory]
    [InlineData(false, "\n\n    \n    AX\n\n    \n    AY\n\n")]
    [InlineData(true, "AXAY")]
    public void Template_Should_Render_Set_Inside_Loop_With_LstripBlocks(bool lstripBlocks, string expected)
    {
        const string templateString = @"
{% for item in items %}
    {% set foo = 'A' + item %}
    {{ foo }}
{% endfor %}
";
        var lexerConfig = new LexerConfig { LstripBlocks = lstripBlocks };

        var lexer = new Lexer(templateString, lexerConfig);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Lexer Tokens:", tokens));

        var template = new Template(templateString, lexerConfig);

        // Debug: Inspect the lexer output

        var result = template.Render(new { items = new[] { "X", "Y" } });
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("{{ \"\\u0000\" }}", "\0")]
    [InlineData("{{ \"\\u2668\" }}", "\u2668")]
    [InlineData("{{ \"\\u00e4\" }}", "\u00e4")]
    [InlineData("{{ \"\\xe4\" }}", "\u00e4")] // \xe4 should also produce 'ä'
    [InlineData("{{ \"\\t\" }}", "\t")]
    [InlineData("{{ \"\\r\" }}", "\r")]
    [InlineData("{{ \"\\n\" }}", "\n")]
    [InlineData("{{ \"\\N{HOT SPRINGS}\" }}", "\u2668")]
    public void Template_Should_Render_String_Escapes(string template, string expected)
    {
        var result = new Template(template).Render();
        result.Should().Be(expected);
    }

    [Fact]
    public void Template_Should_Render_With_Filters()
    {
        var template = new Template("{{ message | upper }} and {{ message | lower }}");
        var result = template.Render(new { message = "Hello World" });
        result.Should().Be("HELLO WORLD and hello world");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_Comments()
    {
        const string templateString = "A {#- comment -#} B";

        // Debug: Inspect the lexer output
        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Lexer Tokens:", tokens));

        var template = new Template(templateString);
        var result = template.Render(new { });
        result.Should().Be("AB");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_For_Block()
    {
        var template = new Template("A {%- for x in xs -%}{{- x -}}{%- endfor -%} B");
        var result = template.Render(new { xs = new[] { "1", "2" } });
        result.Should().Be("A12B");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_Only_Left_Or_Right_Dash()
    {
        var template = new Template("A {%- if true %}X{% endif -%} B");
        var result = template.Render(new { });
        result.Should().Be("AX B");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_Set_Block()
    {
        var template = new Template("A {%- set foo = 'X' -%}{{ foo }} B");
        var result = template.Render(new { });
        result.Should().Be("AXB");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_Tag_Dash()
    {
        // Arrange: Create a template with whitespace trimming using tag dashes
        const string templateString = "Hello    {%- if true -%} World{%- endif -%}    !";


        // Debug: Inspect the lexer output
        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Lexer Tokens:", tokens));

        //_output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateString);

        // Debug: Inspect the AST structure
        //_output.WriteLine("AST Structure:");
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));
        //_output.WriteLine(astDump);

        // Act
        var result = template.Render(new { });

        // Assert
        result.Should().Be("Hello World!");
    }

    [Fact]
    public void Template_Should_Trim_Whitespace_With_Variable_Dash()
    {
        var template = new Template("A\n  {{- value -}}\nB");
        var result = template.Render(new { value = "X" });
        result.Should().Be("AXB");
    }

    [Fact]
    public void Render_SystemMessageTemplate_WhenFirstMessageIsSystemRole_ShouldReturnCorrectFormat()
    {
        var templateStr = """
        {%- if messages[0].role == 'system' %}
            {{- '<|im_start|>system\n' + messages[0].content + '<|im_end|>\n' }}
        {%- endif %}
        """;
        var template = new Template(templateStr);
        var data = new Dictionary<string, object>()
        {
            ["messages"] = new List<object>
            {
                new Dictionary<string, string>()
                {
                    ["role"] = "system",
                    ["content"] = "system promote"
                }
            }
        };

        var result = template.Render(data);

        result.Should().Be("<|im_start|>system\nsystem promote<|im_end|>\n\n");
    }
}