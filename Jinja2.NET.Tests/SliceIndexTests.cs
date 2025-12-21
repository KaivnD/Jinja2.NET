using FluentAssertions;

namespace Jinja2.NET.Tests;

public class SliceIndexTests
{
    [Fact]
    public void List_Slice_Positive_Start_Stop()
    {
        var template = new Template("{{ items[1:3] | join(', ') }}");
        var result = template.Render(new { items = new[] { "a", "b", "c", "d" } });
        result.Should().Be("b, c");
    }

    [Fact]
    public void List_Slice_Omitted_Start()
    {
        var template = new Template("{{ items[:2] | join(', ') }}");
        var result = template.Render(new { items = new[] { "a", "b", "c" } });
        result.Should().Be("a, b");
    }

    [Fact]
    public void List_Slice_Omitted_Stop()
    {
        var template = new Template("{{ items[2:] | join(', ') }}");
        var result = template.Render(new { items = new[] { "a", "b", "c", "d" } });
        result.Should().Be("c, d");
    }

    [Fact]
    public void List_Slice_Negative_Indices()
    {
        var template = new Template("{{ items[-2:] | join(', ') }}");
        var result = template.Render(new { items = new[] { "a", "b", "c", "d" } });
        result.Should().Be("c, d");
    }

    [Fact]
    public void List_Slice_With_Step()
    {
        var template = new Template("{{ items[0:4:2] | join(', ') }}");
        var result = template.Render(new { items = new[] { "a", "b", "c", "d" } });
        result.Should().Be("a, c");
    }

    [Fact]
    public void String_Slice_Returns_Substring()
    {
        var template = new Template("{{ s[1:4] }}");
        var result = template.Render(new { s = "Hello" });
        result.Should().Be("ell");
    }
}
