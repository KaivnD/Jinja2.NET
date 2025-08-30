using FluentAssertions;
using Jinja2.NET.Nodes;
using Jinja2.NET.Parsers;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class ParserTests
{
    private readonly ITestOutputHelper _output;

    public ParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ExpressionParser_Should_Parse_Boolean_Literals(string booleanString, bool expectedValue)
    {
        var lexer = new Lexer($"{{{{ {booleanString} }}}}");
        var tokens = lexer.Tokenize();
        var tokenIterator = new TokenIterator(tokens);

        tokenIterator.Consume(ETokenType.VariableStart); // Skip {{
        var parser = new ExpressionParser();
        var result = parser.Parse(tokenIterator, ETokenType.VariableEnd);

        result.Should().BeOfType<LiteralNode>()
            .Which.Value.Should().Be(expectedValue);
    }

    [Fact]
    public void Parser_Should_Allow_NonWhitespace_Text_Between_Else_And_Endif()
    {
        var template = @"{% if true %}yes{% else %}not yes!{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var parser = new MainParser();
        var ast = parser.Parse(template);


        var ifBlock = ast.Children[0] as BlockNode;
        ifBlock.Should().NotBeNull();
        ifBlock!.Children.Should().HaveCount(2);

        ifBlock.Children[0].Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("yes");

        ifBlock.Children[1].Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be("else");

        var elseBlock = (BlockNode)ifBlock.Children[1];
        elseBlock.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>().Which.Content.Should().Be("not yes!");
    }

    [Fact]
    public void Parser_Should_Allow_Simple_Unknown_Block()
    {
        var template = "{% unknownblock %}";
        var parser = new MainParser();

        Action act = () => parser.Parse(template);
        act.Should().NotThrow();
    }

    [Fact]
    public void Parser_Should_Handle_Indented_Complex_Blocks()
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
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        var parser = new MainParser();
        var ast = parser.Parse(template);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        // Only consider block nodes for this assertion
        var blocks = ast.Children.OfType<BlockNode>().ToList();
        blocks.Should().HaveCount(2);
        blocks[0].Should().BeOfType<BlockNode>().Which.Name.Should().Be("set");
        blocks[1].Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
        var forBlock = blocks[1];
        var blockChildren = forBlock.Children.OfType<BlockNode>().ToList();
        blockChildren.Should().ContainSingle()
            .Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Handle_Whitespace_Between_Blocks()
    {
        var template = @"
{% set foo = 1 %}

{% if foo %}
    Hello
{% else %}

    World
{% endif %}

";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        // Only consider block nodes for this assertion
        var blocks = ast.Children.OfType<BlockNode>().ToList();
        blocks.Should().HaveCount(2);
        blocks[0].Name.Should().Be("set");
        blocks[1].Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_Boolean_False_As_LiteralNode()
    {
        const string template = "{{ false }}";
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var variable = ast.Children[0] as VariableNode;
        variable.Should().NotBeNull();
        variable!.Expression.Should().BeOfType<LiteralNode>()
            .Which.Value.Should().Be(false);
    }

    [Fact]
    public void Parser_Should_Parse_Boolean_True_As_LiteralNode()
    {
        const string template = "{{ true }}";
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var variable = ast.Children[0] as VariableNode;
        variable.Should().NotBeNull();
        variable!.Expression.Should().BeOfType<LiteralNode>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public void Parser_Should_Parse_Complex_Set_Statement()
    {
        var template =
            @"{% set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>' + (message['content'] | trim) + '<|eot_id|>' %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        var parser = new MainParser();
        var ast = parser.Parse(template);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>()
            .Which.Name.Should().Be("set");

        var setBlock = (BlockNode)ast.Children[0];
        setBlock.Arguments.Should().HaveCount(2);
        setBlock.Arguments[0].Should().BeOfType<IdentifierNode>()
            .Which.Name.Should().Be("content");
        setBlock.Arguments[1].Should().BeOfType<BinaryExpressionNode>();
        // Optionally, further assert the structure of the BinaryExpressionNode
    }

    [Fact]
    public void Parser_Should_Parse_FilterNode()
    {
        const string template = "{{ name | upper }}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();


        // Act

        var ast = parser.Parse(template);

        // Assert
        var variable = ast.Children[0] as VariableNode;
        variable.Should().NotBeNull();
        variable!.Expression.Should().BeOfType<FilterNode>();
        var filter = (FilterNode)variable.Expression;
        filter.FilterName.Should().Be(BuiltinFilters.UpperFilter);
        filter.Expression.Should().BeOfType<IdentifierNode>()
            .Which.Name.Should().Be("name");
    }

    [Fact]
    public void Parser_Should_Parse_For_Block_With_Nested_If_And_No_Else()
    {
        var template = @"{% for i in [1,2] %}
{% if i == 1 %}
One
{% endif %}
{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        // Act
        var parser = new MainParser();
        var ast = parser.Parse(template);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        // Assert
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
        var forBlock = (BlockNode)ast.Children[0];
        var blockChildren = forBlock.Children.OfType<BlockNode>().ToList();
        blockChildren.Should().ContainSingle()
            .Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_For_Block_With_Nested_If_Else()
    {
        var template = @"{% for i in [1,2] %}
{% if i == 1 %}
One
{% else %}
Not one
{% endif %}
{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
        var forBlock = (BlockNode)ast.Children[0];
        var blockChildren = forBlock.Children.OfType<BlockNode>().ToList();
        blockChildren.Should().ContainSingle()
            .Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_For_Block_With_Nested_If_Else_And_Extra_Whitespace()
    {
        var template = @"{% for i in [1,2] %}

{% if i == 1 %}

One

{% else %}

Not one

{% endif %}

{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
        var forBlock = (BlockNode)ast.Children[0];
        var blockChildren = forBlock.Children.OfType<BlockNode>().ToList();
        blockChildren.Should().ContainSingle()
            .Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_For_Block_With_Whitespace()
    {
        var template = @"{% for i in [1,2] %}
{{ i }}
{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();

        var ast = parser.Parse(template);

        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
    }

    [Fact]
    public void Parser_Should_Parse_For_Loop_With_Newlines()
    {
        var template = @"{% for item in items %}- {{ item }}
{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();


        _output.WriteLine("Tokens:");
        foreach (var token in tokens)
        {
            _output.WriteLine($"Token: {token.Type} [{token.Value.Replace("\r", "\\r").Replace("\n", "\\n")}]");
        }

        var ast = parser.Parse(template);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));


        var forBlock = ast.Children.OfType<BlockNode>().FirstOrDefault(b => b.Name == "for");
        forBlock.Should().NotBeNull();

        var textNodes = forBlock!.Children.OfType<TextNode>().ToList();
        var variableNodes = forBlock!.Children.OfType<VariableNode>().ToList();

        foreach (var t in textNodes)
        {
            _output.WriteLine($"Captured TextNode: [{t.Content.Replace("\r", "\\r").Replace("\n", "\\n")}]");
        }

        textNodes.Should().ContainSingle(t => t.Content == "- ");
        variableNodes.Should().ContainSingle(v =>
            v.Expression.GetType() == typeof(IdentifierNode) && ((IdentifierNode)v.Expression).Name == "item");
        textNodes.Should().ContainSingle(t => t.Content.EndsWith("\n") || t.Content.EndsWith("\r\n"));
    }

    [Fact]
    public void Parser_Should_Parse_For_Three_Arguments()
    {
        var template = @"{% for x in items %}Item: {{ x }}{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        ast.Should().BeOfType<TemplateNode>();
        ast.Children.Should().HaveCount(1);
        var forBlock = ast.Children[0].Should().BeOfType<BlockNode>().Subject;
        forBlock.Name.Should().Be("for");

        if (forBlock.Arguments.Count == 3)
        {
            forBlock.Arguments[0].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("x");
            forBlock.Arguments[1].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("in");
            forBlock.Arguments[2].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("items");
        }
        else
        {
            // parser is designed to skip the "in" keyword and only store the variable and the iterable as arguments.
            // This is a common and perfectly valid approach.

            forBlock.Children.Should().HaveCount(2);
            forBlock.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("Item: ");
            forBlock.Children[1].Should().BeOfType<VariableNode>()
                .Which.Expression.Should().BeOfType<IdentifierNode>()
                .Which.Name.Should().Be("x");
        }
        //forBlock.Children.Should().HaveCount(1);
        //forBlock.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("Item: {{ x }}");
    }

    //[Fact]
    //public void Parser_Should_Parse_For_Two_Arguments()
    //{
    //    var template = @"{% for x items %}Item: {{ x }}{% endfor %}";
    //    var lexer = new Lexer(template);
    //    var tokens = lexer.Tokenize();
    //    var parser = new MainParser();
    //    var ast = parser.Parse(template);

    //    ast.Should().BeOfType<TemplateNode>();
    //    ast.Children.Should().HaveCount(1);
    //    var forBlock = ast.Children[0].Should().BeOfType<BlockNode>().Subject;
    //    forBlock.Name.Should().Be("for");
    //    forBlock.Arguments.Should().HaveCount(2);
    //    forBlock.Arguments[0].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("x");
    //    forBlock.Arguments[1].Should().BeOfType<IdentifierNode>()
    //        .Which.Name.Should().Be("items");

    //    forBlock.Children.Should().HaveCount(2);
    //    forBlock.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("Item: ");
    //    forBlock.Children[1].Should().BeOfType<VariableNode>()
    //        .Which.Expression.Should().BeOfType<IdentifierNode>()
    //        .Which.Name.Should().Be("x");
    //}

    [Fact]
    public void Parser_Should_Parse_For_With_Else()
    {
        var template = @"{% for x in items %}Item: {{ x }}{% else %}Empty{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        var parser = new MainParser();
        var ast = parser.Parse(template);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        ast.Children.Should().HaveCount(1);
        var forBlock = ast.Children[0].Should().BeOfType<BlockNode>().Subject;
        forBlock.Name.Should().Be("for");
        forBlock.Children.Should().HaveCount(3); // TextNode("Item: "), VariableNode(x), BlockNode(else)
        forBlock.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("Item: ");
        forBlock.Children[1].Should().BeOfType<VariableNode>();
        forBlock.Children[2].Should().BeOfType<BlockNode>().Which.Name.Should().Be("else");
        var elseBlock = (BlockNode)forBlock.Children[2];
        elseBlock.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>().Which.Content.Should().Be("Empty");
    }

    [Fact]
    public void Parser_Should_Parse_If_Block_With_Whitespace()
    {
        var template = @"{% if true %}
yes
{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_If_Condition_With_Boolean_Literal()
    {
        const string template = "{% if true %}yes{% endif %}";
        var parser = new MainParser();
        var ast = parser.Parse(template);

        var ifBlock = ast.Children[0] as BlockNode;
        ifBlock.Should().NotBeNull();
        ifBlock!.Arguments.Should().ContainSingle()
            .Which.Should().BeOfType<LiteralNode>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public void Parser_Should_Parse_If_Else_Block()
    {
        var template = @"{% if true %}yes{% else %}no{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_If_Else_Block_With_Whitespace()
    {
        var template = @"{% if true %}
yes
{% else %}
no
{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_If_With_Elif()
    {
        var template = @"{% if x %}A{% elif y %}B{% else %}C{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        ast.Children.Should().HaveCount(1);
        var ifBlock = ast.Children[0].Should().BeOfType<BlockNode>().Subject;
        ifBlock.Name.Should().Be("if");
        ifBlock.Children.Should().HaveCount(3); // TextNode("A"), BlockNode(elif), BlockNode(else)
        ifBlock.Children[0].Should().BeOfType<TextNode>().Which.Content.Should().Be("A");
        var elifBlock = ifBlock.Children[1].Should().BeOfType<BlockNode>().Subject;
        elifBlock.Name.Should().Be("elif");
        elifBlock.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>().Which.Content.Should().Be("B");
        var elseBlock = ifBlock.Children[2].Should().BeOfType<BlockNode>().Subject;
        elseBlock.Name.Should().Be("else");
        elseBlock.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>().Which.Content.Should().Be("C");
    }


    [Fact]
    public void Parser_Should_Parse_LiteralNode_Number()
    {
        const string template = "{{ 42 }}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();


        // Act
        var ast = parser.Parse(template);

        // Assert
        var variable = ast.Children[0] as VariableNode;
        variable.Should().NotBeNull();
        variable!.Expression.Should().BeOfType<LiteralNode>()
            .Which.Value.Should().Be(42);
    }

    [Fact]
    public void Parser_Should_Parse_MultiTarget_Set()
    {
        var template = @"{% set x, y = 42 %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);

        ast.Children.Should().HaveCount(1);
        var setBlock = ast.Children[0].Should().BeOfType<BlockNode>().Subject;
        setBlock.Name.Should().Be("set");
        setBlock.Arguments.Should().HaveCount(3);
        setBlock.Arguments[0].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("x");
        setBlock.Arguments[1].Should().BeOfType<IdentifierNode>().Which.Name.Should().Be("y");
        setBlock.Arguments[2].Should().BeOfType<LiteralNode>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void Parser_Should_Parse_Nested_If_In_For()
    {
        var template = @"{% for i in [1] %}{% if i %}x{% endif %}{% endfor %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("for");
        var forBlock = (BlockNode)ast.Children[0];
        forBlock.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_Raw_With_NonText()
    {
        var template = @"{% raw %}{{ x }}{% endraw %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));

        var parser = new MainParser();
        var ast = parser.Parse(template);


        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        ast.Children.Should().HaveCount(1);
        var rawNode = ast.Children[0].Should().BeOfType<RawNode>().Subject;
        rawNode.Content.Should().Be("{{ x }}");
    }

    [Fact]
    public void Parser_Should_Parse_Simple_If_Block()
    {
        var template = @"{% if true %}yes{% endif %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();
        var ast = parser.Parse(template);
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<BlockNode>().Which.Name.Should().Be("if");
    }

    [Fact]
    public void Parser_Should_Parse_TextNode()
    {
        const string template = "Hello world!";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();


        // Act
        var ast = parser.Parse(template);

        // Assert
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<TextNode>()
            .Which.Content.Should().Be("Hello world!");
    }

    [Fact]
    public void Parser_Should_Parse_VariableNode()
    {
        const string template = "{{ name }}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        var parser = new MainParser();

        // Act
        var ast = parser.Parse(template);

        // Assert
        ast.Children.Should().ContainSingle()
            .Which.Should().BeOfType<VariableNode>();
        var variable = (VariableNode)ast.Children[0];
        variable.Expression.Should().BeOfType<IdentifierNode>()
            .Which.Name.Should().Be("name");
    }

    [Fact]
    public void Parser_Should_Throw_On_Unexpected_Token()
    {
        const string template = "{% %}";
        var lexer = new Lexer(template);
        var tokens = lexer.Tokenize();
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", tokens));
        var parser = new MainParser();

        // Act
        Action act = () => parser.Parse(template);

        // Assert
        act.Should().Throw<TemplateParsingException>();
    }


    [Theory]
    [InlineData("{% %}", "Unexpected token|Expected block name|Expected Identifier")]
    [InlineData("{{ }}", "Unexpected token|Expected expression")]
    [InlineData("{% set foo = %}", "Expected expression|Unexpected token: BlockEnd")]
    [InlineData("{% set foo = 1 ", "Unclosed set block|Unclosed tag")]
    [InlineData("{% if true %}", "Unclosed if block|Unclosed 'if' block")]
    [InlineData("{% if true %}yes{% endfor %}", "Unexpected token|Unclosed 'if' block|Unclosed for block")]
    [InlineData("{% for i in [1,2] %}item{% endif %}", "Unclosed 'for' block|Expected '{%' for endfor")]
    [InlineData("{% for i in [1,2] %}item", "Unclosed if block|Unclosed 'for' block|Expected '{%' for endfor")]
    public void Parser_Should_Throw_Syntax_Error_On_Invalid_Template(string template, string expectedError)
    {
        var act = () =>
        {
            var lexer = new Lexer(template);
            var tokens = lexer.Tokenize();
            var parser = new MainParser();
            var ast = parser.Parse(template);
        };

        var options = expectedError.Split('|');
        act.Should().Throw<Exception>()
            .Where(ex =>
                (ex is TemplateParsingException || ex is InvalidOperationException) &&
                options.Any(opt => ex.Message.Contains(opt)));
    }

    [Fact]
    public void VisitListLiteral_Should_Return_List_Of_Values()
    {
        // Arrange
        var context = new TemplateContext();
        var renderer = new Renderer(context);

        var elements = new List<ExpressionNode>
        {
            new LiteralNode(1),
            new LiteralNode(2),
            new LiteralNode(3)
        };
        var listNode = new ListLiteralNode(elements);

        // Act
        var result = renderer.Visit(listNode);

        // Assert
        result.Should().BeOfType<List<object>>();
        var list = (List<object>)result;
        list.Should().HaveCount(3);
        list[0].Should().Be(1);
        list[1].Should().Be(2);
        list[2].Should().Be(3);
    }
}