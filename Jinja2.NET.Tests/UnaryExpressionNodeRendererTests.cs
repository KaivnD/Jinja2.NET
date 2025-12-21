using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Jinja2.NET;

namespace Jinja2.NET.Tests;

public class UnaryExpressionNodeRendererTests
{
    [Fact]
    public void NotOperator_Should_InvertTruthiness()
    {
        var tpl = new Template("{{ not true }}");
        var output = tpl.Render();
        output.Should().Be("False");

        tpl = new Template("{{ not 0 }}");
        output = tpl.Render();
        output.Should().Be("True");

        tpl = new Template("{{ not '' }}");
        output = tpl.Render();
        output.Should().Be("True");
    }

    [Fact]
    public void UnaryMinus_Should_NegateNumber()
    {
        var tpl = new Template("{{ -5 }}");
        var output = tpl.Render();
        output.Should().Be("-5");

        tpl = new Template("{{ -3.5 }}");
        output = tpl.Render();
        output.Should().Be("-3.5");
    }
}
