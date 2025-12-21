using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Jinja2.NET;

namespace Jinja2.NET.Tests;

public class BinaryExpressionNodeRendererTests
{
    [Fact]
    public void Addition_And_Concatenation_Work()
    {
        var tpl = new Template("{{ 2 + 3 }}");
        tpl.Render().Should().Be("5");

        tpl = new Template("{{ 'a' + 'b' }}");
        tpl.Render().Should().Be("ab");
    }

    [Fact]
    public void CompareOperators_Should_Evaluate()
    {
        var tpl = new Template("{{ 1 < 2 }}");
        tpl.Render().Should().Be("True");

        tpl = new Template("{{ 2 >= 2 }}");
        tpl.Render().Should().Be("True");
    }

    [Fact]
    public void InOperator_Works_ForStrings_And_Lists()
    {
        var tpl = new Template("{{ 'a' in 'abc' }}");
        tpl.Render().Should().Be("True");

        tpl = new Template("{{ 2 in [1,2,3] }}");
        tpl.Render().Should().Be("True");
    }

    [Fact]
    public void IsOperator_Defined_And_NotDefined()
    {
        var tpl = new Template("{{ x is defined }}");
        tpl.Render().Should().Be("False");

        tpl = new Template("{{ x is defined }}");
        var output = tpl.Render(new Dictionary<string, object> { { "x", 1 } });
        output.Should().Be("True");

        tpl = new Template("{{ x is not defined }}");
        tpl.Render().Should().Be("True");
    }
}
