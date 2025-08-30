using FluentAssertions;
using Jinja2.NET.Tests.Helpers;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Integrations;

public class SetTagScopingTests
{
    private readonly ITestOutputHelper _output;

    public SetTagScopingTests(ITestOutputHelper output)
    {
        _output = output;
        TestTraceSetup.EnsureListener(output);
    }

    [Fact]
    public void Set_Invalid_Arguments_Throws()
    {
        const string templateString = @"{% set x %}{% for i in [1,2] %}{{ i }}{% endfor %}";

        // Act: Wrap the constructor call if that's where the exception is thrown
        var exception = Record.Exception(() => new Template(templateString));

        // Assert
        exception.Should().BeOfType<TemplateParsingException>();
        // exception.Message.Should().Contain("Expected '=' in set block");
    }


    [Fact]
    public void Set_Reassignment_Inside_Empty_For()
    {
        // Arrange
        const string templateString = @"{% set x = 1 %}{% for i in [] %}{% set x = i %}{{ x }}{% endfor %}{{ x }}";
        var template = new Template(templateString);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));

        // Act
        var result = template.Render();

        // Assert
        result.Should().Be("1", "because empty loop should not change outer x");
    }


    [Fact]
    public void Set_Reassignment_Inside_For_Affects_Inner_Usage()
    {
        const string templateString = @"{% set x = 1 %}{% for i in [1,2] %}{% set x = i %}{{ x }}{% endfor %}{{ x }}";
        var parser = new DebugExpressionMainParser(_output);
        var ast = parser.Parse(templateString);
        _output.WriteLine("---Additional info----");
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", parser.GetLastTokens()));
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        var loggingContext = new LoggingTemplateContext(_output);

        var renderer = new Renderer(loggingContext);
        var result = renderer.Render(ast);
        _output.WriteLine($"Rendered Output: {result}");

        result.Should().Be("121", "because set inside for loop should only affect the current iteration, not the outer scope");
    }

    [Fact]
    public void Set_Reassignment_Inside_For_Does_Not_Leak_Outside_Loop()
    {
        // Arrange
        const string templateString = @"{% set x = 1 %}{% for i in [1,2] %}{% set x = 2 %}{% endfor %}{{ x }}";
        var template = new Template(templateString);

        // Act
        var result = template.Render();

        // Assert
        result.Should().Be("1");
    }

    //[Fact]
    //public void Set_Reassignment_Inside_For_LoopScoped()
    //{
    //    // Arrange
    //    const string templateString =
    //        @"{% set x = 1 %}{% for i in [1,2] %}{% set x = i loop %}{{ x }}{% endfor %}{{ x }}";
    //    var template = new Template(templateString);
    //    _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));

    //    // Act
    //    var result = template.Render();

    //    // Assert
    //    result.Should().Be("111", "because loop-scoped set should not affect outer scope");
    //}

    [Fact]
    public void Set_Reassignment_Inside_For_LoopScoped()
    {
        // Arrange
        const string templateString =
            @"{% set x = 1 %}{% for i in [1,2] %}{% set x = i loop %}{{ x }}{% endfor %}{{ x }}";
        var parser = new DebugExpressionMainParser(_output);
        var ast = parser.Parse(templateString);
        _output.WriteLine("---Additional info----");
        _output.WriteLine(TemplateDebugger.DebugTokens("Tokens:", parser.GetLastTokens()));
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", ast));

        var loggingContext = new LoggingTemplateContext(_output);

        // Act
        var renderer = new Renderer(loggingContext);
        var result = renderer.Render(ast);
        _output.WriteLine($"Rendered Output: {result}");

        // Assert
        result.Should().Be("121", "because loop-scoped set should not affect outer scope");
    }

    [Fact]
    public void Set_Reassignment_Inside_If_Does_Not_Leak_Outside_If()
    {
        // Arrange
        const string templateString = @"{% set x = 1 %}{% if true %}{% set x = 2 %}{% endif %}{{ x }}";
        var template = new Template(templateString);

        // Act
        var result = template.Render();

        // Assert
        result.Should().Be("2");
    }

    [Fact]
    public void Set_Reassignment_Inside_Nested_For()
    {
        // Arrange
        const string templateString =
            @"{% set x = 1 %}{% for i in [1,2] %}{% for j in [3,4] %}{% set x = j %}{{ x }}{% endfor %}{% endfor %}{{ x }}";
        var template = new Template(templateString);
        _output.WriteLine(TemplateDebugger.DebugAst("AST:", template.Ast));

        // Act
        var result = template.Render();

        // Assert
        result.Should().Be("34344", "because set in nested loop should update outer scope");
    }
}