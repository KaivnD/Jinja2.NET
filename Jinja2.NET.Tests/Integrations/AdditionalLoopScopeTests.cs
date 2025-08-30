using System.Diagnostics;
using FluentAssertions;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class AdditionalLoopScopeTests
{
    private readonly TemplateContext _context = new();
    private readonly ITestOutputHelper _output;
    private readonly Renderer _renderer;
    private readonly ScopeManager _scopeManager = new();

    public AdditionalLoopScopeTests(ITestOutputHelper output)
    {
        _output = output;
        Trace.Listeners.Add(new StringBuilderTraceListener());
        _renderer = new Renderer(_context, _scopeManager);
    }

    [Fact]
    public void Deep_Nested_Loops()
    {
        // Arrange
        const string templateString =
            @"{% set x = 1 %}{% for i in [1] %}{% for j in [2] %}{% for k in [3] %}{% set x = k %}{{ x }}{% endfor %}{{ x }}{% endfor %}{{ x }}{% endfor %}{{ x }}";
        var template = new Template(templateString);

        // Act
        //var result = _renderer.Render(template.Ast);
        var result = template.Render();  // Remove the custom _renderer.Render(template.Ast)

        // In the test, add this after template.Render():
        //_output.WriteLine($"Template has {template.Ast.Children.Count} children");
        //for (int i = 0; i < template.Ast.Children.Count; i++)
        //{
        //    _output.WriteLine($"Child {i}: {template.Ast.Children[i].GetType().Name}");
        //}
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Lexer Tokens:", template.Tokens.ToList()));


        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));

        // Assert
        result.Should().Be("3311", "because set in triple-nested loop should update middle scope, not global");
    }

    [Fact]
    public void Empty_Iterable_Renders_Else()
    {
        // Arrange
        const string templateString = @"{% for i in [] %}loop{% else %}empty{% endfor %}";
        var template = new Template(templateString);

        // Act
        var result = _renderer.Render(template.Ast);

        // Assert
        result.Should().Be("empty", "because empty iterable should render else block");
    }

    [Fact]
    public void Multiple_Loop_Variables()
    {
        // Arrange
        const string templateString = @"{% for k, v in items %}{{ k }}:{{ v }}{% endfor %}";
        var template = new Template(templateString);
        _context.Set("items", new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }
            .Select(kvp => new object[] { kvp.Key, kvp.Value }));

        // Act
        var result = _renderer.Render(template.Ast);

        // Assert
        result.Should().Be("a:1b:2", "because loop should unpack multiple variables correctly");
    }

    [Fact]
    public void Nested_Loops_Without_Set()
    {
        // Arrange
        const string templateString =
            @"{% set x = 1 %}{% for i in [1,2] %}{% for j in [3,4] %}{{ x }}{% endfor %}{{ x }}{% endfor %}{{ x }}";
        var template = new Template(templateString);

        // Act
        var result = _renderer.Render(template.Ast);

        // Assert
        result.Should().Be("1111111", "because no set in loops should preserve global x");
    }

    [Fact]
    public void Recursive_Loop()
    {
        // Arrange
        const string templateString =
            @"{% for item in items recursive %}{{ item.name }}{% if item.children %}<ul>{{ loop(item.children) }}</ul>{% endif %}{% endfor %}";
        var template = new Template(templateString);

        // Fix: Explicitly type the array
        _context.Set("items", new object[]
        {
            new { name = "A", children = new object[] { new { name = "B", children = new object[] { } } } },
            new { name = "C", children = new object[] { } }
        });

        // Act
        var result = _renderer.Render(template.Ast);
        _output.WriteLine(TemplateDebugger.GetExtendedTokensInfo("Lexer Tokens:", template.Tokens.ToList()));


        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));
        // Assert
        result.Should().Be("A<ul>B</ul>C", "because recursive loop should render nested structures");
    }

    [Fact]
    public void Variable_Shadowing()
    {
        // Arrange
        const string templateString = @"{% set x = 1 %}{% for x in [2] %}{{ x }}{% endfor %}{{ x }}";
        var template = new Template(templateString);

        // Act
        var result = _renderer.Render(template.Ast);

        // Assert
        result.Should().Be("21", "because loop variable should shadow global variable temporarily");
    }
}