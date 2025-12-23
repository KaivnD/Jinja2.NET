using FluentAssertions;
using Xunit;

namespace Jinja2.NET.Tests;

public class MacroTests
{
    [Fact]
    public void Macro_Should_Define_And_Invoke_Without_Args()
    {
        var tpl = "{% macro hello() %}Hello World{% endmacro %}{{ hello() }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("Hello World");
    }

    [Fact]
    public void Macro_Should_Pass_Arguments()
    {
        var tpl = "{% macro greet(name) %}Hello {{ name }}!{% endmacro %}{{ greet('Alice') }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("Hello Alice!");
    }

    [Fact]
    public void Macro_Should_Support_Multiple_Arguments()
    {
        var tpl = "{% macro join(a, b, c) %}{{ a }}-{{ b }}-{{ c }}{% endmacro %}{{ join('x','y','z') }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("x-y-z");
    }

    [Fact]
    public void Macro_Nested_Macro_Calls()
    {
        var tpl = "{% macro inner(v) %}inner:{{ v }}{% endmacro %}{% macro outer(w) %}outer-{{ inner(w) }}{% endmacro %}{{ outer('ok') }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("outer-inner:ok");
    }

    [Fact]
    public void Macro_Recursive_Countdown()
    {
        var tpl = "{% macro countdown(n) %}{% if n > 0 %}{{ n }} {% countdown(n-1) %}{% endif %}{% endmacro %}{{ countdown(3) }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("3 2 1 ");
    }

    [Fact]
    public void Macro_Local_Variable_Is_Isolated()
    {
        var tpl = "{% set a = 'outside' %}{% macro m() %}{% set a = 'inside' %}{{ a }}{% endmacro %}{{ m() }}-{{ a }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("inside-outside");
    }

    [Fact]
    public void Macro_Missing_EndMacro_Throws_ParseException()
    {
        var tpl = "{% macro bad() %}no end";
        Action act = () => new Template(tpl);
        act.Should().Throw<TemplateParsingException>();
    }

    [Fact]
    public void Macro_Invoke_Undefined_Throws_On_Render()
    {
        var tpl = "{{ unknown_macro() }}";
        var template = new Template(tpl);
        Action act = () => template.Render();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Macro_Many_Parameters()
    {
        var tpl = "{% macro many(a,b,c,d,e,f,g,h,i,j) %}{{ a }}|{{ b }}|{{ c }}|{{ d }}|{{ e }}|{{ f }}|{{ g }}|{{ h }}|{{ i }}|{{ j }}{% endmacro %}{{ many(1,2,3,4,5,6,7,8,9,10) }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("1|2|3|4|5|6|7|8|9|10");
    }

    [Fact]
    public void Macro_Definition_Without_Parentheses_Throws_ParseException()
    {
        var tpl = "{% macro bad %}x{% endmacro %}";
        Action act = () => new Template(tpl);
        act.Should().Throw<TemplateParsingException>();
    }

    [Fact]
    public void Macro_Deep_Recursion()
    {
        var tpl = "{% macro rec(n) %}{% if n <= 0 %}done{% else %}{{ n }} {{ rec(n-1) }}{% endif %}{% endmacro %}{{ rec(5) }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("5 4 3 2 1 done");
    }

    [Fact]
    public void Macro_Empty_Body_Returns_Empty()
    {
        var tpl = "{% macro e() %}{% endmacro %}{{ e() }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("");
    }

    [Fact]
    public void Macro_Redefinition_Uses_Latest()
    {
        var tpl = "{% macro m() %}A{% endmacro %}{% macro m() %}B{% endmacro %}{{ m() }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("B");
    }

    [Fact]
    public void Macro_Argument_Is_Expression()
    {
        var tpl = "{% macro add(x) %}{{ x }}{% endmacro %}{{ add(1+2*2) }}";
        var template = new Template(tpl);
        var result = template.Render();
        result.Should().Be("5");
    }

    [Fact]
    public void Macro_Call_Before_Definition_Throws_On_Render()
    {
        var tpl = "{{ before() }}{% macro before() %}X{% endmacro %}";
        var template = new Template(tpl);
        Action act = () => template.Render();
        act.Should().Throw<NotSupportedException>();
    }
}
