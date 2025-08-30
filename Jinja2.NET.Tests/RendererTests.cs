using FluentAssertions;
using Jinja2.NET.Nodes;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class RendererTests
{
    private readonly ITestOutputHelper _output;

    public RendererTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(typeof(Dictionary<string, object>))]
    [InlineData(typeof(TemplateContext))]
    public void Context_Should_Be_Immutable_When_Copied(Type contextType)
    {
        // Arrange
        object context;
        if (contextType == typeof(Dictionary<string, object>))
        {
            context = new Dictionary<string, object> { ["items"] = new[] { "a", "b", "c" } };
        }
        else if (contextType == typeof(TemplateContext))
        {
            var tc = new TemplateContext();
            tc.Set("items", new[] { "a", "b", "c" });
            context = tc;
        }
        else
        {
            throw new NotSupportedException();
        }

        var template = new Template("{% for item in items %}{{ item }}{% endfor %}");

        // Act: Render with copy (reuseContext: false)
        template.Render(context, false);

        // Assert: Modifying the original context should not affect the rendering context
        if (context is Dictionary<string, object> dict)
        {
            dict["newVar"] = "test";
            // The rendered context should not see this change (if your engine copies dictionaries)
            // You may need to expose a way to check the rendering context if needed
        }
        else if (context is TemplateContext tc)
        {
            tc.Set("newVar", "test");
            // The rendered context should not see this change (if your engine copies TemplateContext)
            // You may need to expose a way to check the rendering context if needed
        }
    }

    [Fact]
    public void Renderer_Should_Preserve_HTML_Structure()
    {
        const string templateString = "<div>\n  {% for item in items %}<span>{{ item }}</span> {% endfor %}\n</div>";

        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateString);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));

        var context = new TemplateContext();
        context.Set("items", new[] { "a", "b" });

        var output = template.Render(context);
        output.Should().Be("<div>\n  <span>a</span> <span>b</span> \n</div>");
    }

    [Fact]
    public void Renderer_Should_Render_AttributeNode()
    {
        var ctx = new TemplateContext();
        ctx.Set("obj", new { foo = "bar" });
        var renderer = new Renderer(ctx);
        var expr = new AttributeNode(new IdentifierNode("obj"), "foo");
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(expr)
        });
        var result = renderer.Render(node);
        result.Should().Be("bar");
    }

    [Fact]
    public void Renderer_Should_Render_BinaryExpressionNode()
    {
        var ctx = new TemplateContext();
        var renderer = new Renderer(ctx);
        var expr = new BinaryExpressionNode(
            new LiteralNode(2),
            "+",
            new LiteralNode(3)
        );
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(expr)
        });
        var result = renderer.Render(node);
        result.Should().Be("5");
    }

    [Fact]
    public void Renderer_Should_Render_BinaryExpressionNode_Equality()
    {
        var ctx = new TemplateContext();
        ctx.Set("a", 1);
        ctx.Set("b", 1);
        var renderer = new Renderer(ctx);
        var expr = new BinaryExpressionNode(new IdentifierNode("a"), "==", new IdentifierNode("b"));
        var node = new TemplateNode(new List<ASTNode> { new VariableNode(expr) });
        var result = renderer.Render(node);
        result.Should().Be("True");
    }

    [Fact]
    public void Renderer_Should_Render_BinaryExpressionNode_Inequality()
    {
        var ctx = new TemplateContext();
        ctx.Set("a", 1);
        ctx.Set("b", 2);
        var renderer = new Renderer(ctx);
        var expr = new BinaryExpressionNode(new IdentifierNode("a"), "!=", new IdentifierNode("b"));
        var node = new TemplateNode(new List<ASTNode> { new VariableNode(expr) });
        var result = renderer.Render(node);
        result.Should().Be("True");
    }

    [Fact]
    public void Renderer_Should_Render_FilterNode()
    {
        var ctx = new TemplateContext();
        var renderer = new Renderer(ctx);
        var expr = new FilterNode(new LiteralNode("abc"), "upper");
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(expr)
        });
        var result = renderer.Render(node);
        result.Should().Be("ABC");
    }

    //    [Fact]
    //    public void Renderer_Should_Render_For_Loop_With_Newlines()
    //    {
    //        var template = @"{% for item in items %}- {{ item }}
    //{% endfor %}";
    //        var lexer = new Lexer(template);
    //        var tokens = lexer.Tokenize();
    //        var parser = new Parser_old(tokens);
    //        var ast = parser.Parse();

    //        var context = new TemplateContext();
    //        var renderer = new Renderer(context);

    //        context.Set("items", new[] { "a", "b", "c" });

    //        var output = renderer.Render(ast).Trim();
    //        var normalized = output.Replace("\r\n", "\n");
    //        normalized.Should().Be("- a\n- b\n- c");
    //    }

    [Theory]
    [InlineData(false, true)] // Copy context
    [InlineData(true, true)] // Reuse context
    [InlineData(false, false)] // Copy with dictionary context
    public void Renderer_Should_Render_For_Loop_With_Newlines(bool reuseContext, bool useTemplateContext)
    {
        // Arrange
        var templateSource = @"{% for item in items %}- {{ item }}
{% endfor %}";

        // Debug: Log tokens and AST
        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        var tokenDebug = TemplateDebugger.DebugTokens("Tokens for For Loop Template:", tokens);
        _output.WriteLine(tokenDebug);

        var template = new Template(templateSource);
        var astDebug = TemplateDebugger.DebugAst("AST for For Loop Template:", template.Ast);
        _output.WriteLine(astDebug);

        object context;
        TemplateContext? templateContext = null;
        if (useTemplateContext)
        {
            templateContext = new TemplateContext();
            templateContext.Set("items", new[] { "a", "b", "c" });
            context = templateContext;
        }
        else
        {
            context = new Dictionary<string, object>
            {
                { "items", new[] { "a", "b", "c" } }
            };
        }

        // Act
        var output = template.Render(context, reuseContext);

        // Assert
        output.Should().Be("- a\n- b\n- c\n");
    }

    [Fact]
    public void Renderer_Should_Render_For_Loop_With_Null_Context()
    {
        // Arrange
        var templateSource = @"{% for item in items %}- {{ item }}
{% endfor %}";

        // Debug: Log tokens and AST
        var lexer = new Lexer(templateSource);
        var tokens = lexer.Tokenize();
        var tokenDebug = TemplateDebugger.DebugTokens("Tokens for For Loop Template:", tokens);
        _output.WriteLine(tokenDebug);

        var template = new Template(templateSource);
        var astDebug = TemplateDebugger.DebugAst("AST for For Loop Template:", template.Ast);
        _output.WriteLine(astDebug);

        // Act
        var output = template.Render();

        // Assert
        output.Should().BeEmpty("because null context should result in no items");
    }

    [Fact]
    public void Renderer_Should_Render_For_Loop_With_Three_Arguments()
    {
        var template = @"{% for i in items %}{{ i }}{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        context.Set("items", new[] { "x", "y", "z" });

        var output = renderer.Render(ast).Trim();
        output.Should().Be("xyz");
    }

    [Fact]
    public void Renderer_Should_Render_For_Three_Arguments()
    {
        var template = @"{% for x in items %}Item: {{ x }}{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        context.Set("items", new[] { "a", "b" });
        var renderer = new Renderer(context);

        var output = renderer.Render(ast);
        output.Should().Be("Item: aItem: b");
    }

    //[Fact]
    //public void Renderer_Should_Render_For_Two_Arguments()
    //{
    //    var template = @"{% for x items %}Item: {{ x }}{% endfor %}";
    //    var lexer = new Lexer(template);
    //    var tokens = lexer.Tokenize();
    //    var parser = new MainParser();
    //    var ast = parser.Parse(template);

    //    var context = new TemplateContext();
    //    context.Set("items", new[] { "a", "b" });
    //    var renderer = new Renderer(context);

    //    var output = renderer.Render(ast);
    //    output.Should().Be("Item: aItem: b");
    //}

    [Fact]
    public void Renderer_Should_Render_For_With_Else_Empty()
    {
        var template = @"{% for x in items %}Item: {{ x }}{% else %}Empty{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        context.Set("items", new string[] { });
        var renderer = new Renderer(context);

        var output = renderer.Render(ast);
        output.Should().Be("Empty");
    }

    [Fact]
    public void Renderer_Should_Render_Html_With_Spaces_Between_Variables()
    {
        var templateSource = "<div> a b </div>";
        var template = new Template(templateSource);
        var context = new TemplateContext();
        var output = template.Render(context);
        output.Should().Be("<div> a b </div>");
    }

    [Fact]
    public void Renderer_Should_Render_Html_With_Spaces_Between_Variables_And_Expressions()
    {
        var templateString = "<div> {{ a }} {{ b }} </div>";
        
        // Debug: Log tokens
        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var template = new Template(templateString);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));


        var context = new TemplateContext();
        context.Set("a", "foo");
        context.Set("b", "bar");
        var output = template.Render(context);

        output.Should().Be("<div> foo bar </div>");
    }

    // Add similar tests for <, >, <=, >=
    [Fact]
    public void Renderer_Should_Render_IdentifierNode()
    {
        var ctx = new TemplateContext();
        ctx.Set("foo", 42);
        var renderer = new Renderer(ctx);
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(new IdentifierNode("foo"))
        });
        var result = renderer.Render(node);
        result.Should().Be("42");
    }

    [Fact]
    public void Renderer_Should_Render_If_Else()
    {
        var templateString = @"{% if foo %}yes{% else %}no{% endif %}";
        var lexer = new Lexer(templateString);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(templateString);

        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        context.Set("foo", true);
        renderer.Render(ast).Trim().Should().Be("yes");

        context.Set("foo", false);
        renderer = new Renderer(context);
        renderer.Render(ast).Trim().Should().Be("no");
    }

    [Fact]
    public void Renderer_Should_Render_If_With_Elif()
    {
        var template = @"{% if x %}A{% elif y %}B{% else %}C{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var parser = new MainParser();
        var ast = parser.Parse(template);


        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));
        var context = new TemplateContext();
        context.Set("x", false);
        context.Set("y", true);
        var renderer = new Renderer(context);

        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));
        var output = renderer.Render(ast);
        output.Should().Be("B");
    }

    [Fact]
    public void Renderer_Should_Render_IndexNode()
    {
        var ctx = new TemplateContext();
        ctx.Set("arr", new[] { "a", "b", "c" });
        var renderer = new Renderer(ctx);
        var expr = new IndexNode(new IdentifierNode("arr"), new LiteralNode(1));
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(expr)
        });
        var result = renderer.Render(node);
        result.Should().Be("b");
    }

    [Fact]
    public void Renderer_Should_Render_ListLiteralNode()
    {
        var ctx = new TemplateContext();
        var renderer = new Renderer(ctx);
        var expr = new ListLiteralNode(new List<ExpressionNode>
        {
            new LiteralNode(1),
            new LiteralNode(2),
            new LiteralNode(3)
        });
        var node = new TemplateNode(new List<ASTNode>
        {
            new VariableNode(expr)
        });
        var result = renderer.Render(node);
        result.Should().Be("[1, 2, 3]");
    }

    [Fact]
    public void Renderer_Should_Render_LiteralNode()
    {
        var ctx = new TemplateContext();
        var renderer = new Renderer(ctx);
        var node = new TemplateNode(new List<ASTNode>
            {
                new VariableNode(new LiteralNode(123))
            }
        );
        var result = renderer.Render(node);
        result.Should().Be("123");
    }

    [Fact]
    public void Renderer_Should_Render_MultiTarget_Set()
    {
        var template = @"{% set x, y = 42 %}{{ x }}|{{ y }}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        var output = renderer.Render(ast);
        output.Should().Be("42|42");
    }

    [Fact]
    public void Renderer_Should_Render_Nested_For_And_If()
    {
        var template = @"{% for i in items %}{% if i %}X{% endif %}{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        context.Set("items", new[] { 0, 1, 2 });
        var output = renderer.Render(ast);
        output.Should().Be("XX");
    }


    [Fact]
    public void Renderer_Should_Render_Raw_With_NonText()
    {
        var template = @"{% raw %}{{ x }}{% endraw %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        context.Set("x", "test");
        var renderer = new Renderer(context);


        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        var output = renderer.Render(ast);
        output.Should().Be("{{ x }}");
    }

    [Fact]
    public void Renderer_Should_Render_Raw_With_Text()
    {
        var template = @"{% raw %}Hello, this is raw text!{% endraw %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        var output = renderer.Render(ast);
        output.Should().Be("Hello, this is raw text!");
    }

    [Fact]
    public void Renderer_Should_Render_Set_Block()
    {
        var template = @"{% set foo = 42 %}{{ foo }}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var context = new TemplateContext();
        var renderer = new Renderer(context);

        var output = renderer.Render(ast).Trim();
        output.Should().Be("42");
    }

    [Fact]
    public void Renderer_Should_Render_TextNode()
    {
        var ctx = new TemplateContext();
        var renderer = new Renderer(ctx);
        var node = new TemplateNode(new List<ASTNode> { new TextNode("abc") });
        var result = renderer.Render(node);
        result.Should().Be("abc");
    }
}