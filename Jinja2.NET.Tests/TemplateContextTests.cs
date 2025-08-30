using FluentAssertions;

namespace Jinja2.NET.Tests;

public class TemplateContextTests
{
    [Fact]
    public void Scope_Push_And_Pop_Should_Work()
    {
        // Arrange
        var ctx = new TemplateContext();
        ctx.Set("x", 1);

        // Act
        ctx.PushScope();
        ctx.Set("x", 2);

        // Assert
        ctx.Get("x").Should().Be(2);

        // Act
        ctx.PopScope();

        // Assert
        ctx.Get("x").Should().Be(1);
    }

    [Fact]
    public void Set_And_Get_Should_Work()
    {
        // Arrange
        var ctx = new TemplateContext();

        // Act
        ctx.Set("foo", 123);

        // Assert
        ctx.Get("foo").Should().Be(123);
    }

    [Fact]
    public void SetAll_Should_Set_Dictionary()
    {
        // Arrange
        var ctx = new TemplateContext();

        // Act
        ctx.SetAll(new Dictionary<string, object> { ["k"] = 42 });

        // Assert
        ctx.Get("k").Should().Be(42);
    }

    [Fact]
    public void SetAll_Should_Set_Properties()
    {
        // Arrange
        var ctx = new TemplateContext();

        // Act
        ctx.SetAll(new { a = 1, b = "x" });

        // Assert
        ctx.Get("a").Should().Be(1);
        ctx.Get("b").Should().Be("x");
    }
}