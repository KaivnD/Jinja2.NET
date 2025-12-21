using FluentAssertions;
using Xunit;

namespace Jinja2.NET.Tests;

public class MethodCallNodeRendererTests
{
    [Fact]
    public void String_Split_With_Separator_Should_Return_Parts()
    {
        var template = new Template("{{ value.split('-') | join(',') }}");
        var result = template.Render(new { value = "a-b-c" });
        result.Should().Be("a,b,c");
    }

    [Fact]
    public void String_Split_Default_Should_Split_On_Whitespace()
    {
        var template = new Template("{{ value.split() | join(',') }}");
        var text = "hello  world\tfoo\nbar";
        var result = template.Render(new { value = text });
        result.Should().Be("hello,world,foo,bar");
    }

    [Fact]
    public void Invoke_Instance_Method_Via_Reflection_Should_Return_Value()
    {
        var template = new Template("{{ value.Substring(1,3) }}");
        var result = template.Render(new { value = "hello" });
        result.Should().Be("ell");
    }

    [Fact]
    public void Calling_Method_On_Null_Should_Throw_NullReferenceException()
    {
        var template = new Template("{{ obj.ToString() }}");
        Action act = () => template.Render(new Dictionary<string, object?> { ["obj"] = null });
        act.Should().Throw<System.NullReferenceException>();
    }

    [Fact]
    public void Calling_Nonexistent_Method_Should_Throw_InvalidOperationException()
    {
        var template = new Template("{{ value.Foo(1) }}");
        Action act = () => template.Render(new { value = "x" });
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*Method 'Foo' not found*", because: "method should not exist");
    }
}
