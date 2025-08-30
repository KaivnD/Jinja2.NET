using FluentAssertions;

namespace Jinja2.NET.Tests;

public class BuiltinFiltersTests
{
    [Fact]
    public void Default_Filter_Should_Return_Default_If_Null()
    {
        var result = BuiltinFilters.ApplyFilter(BuiltinFilters.DefaultFilter, null, new object[] { "x" });
        result.Should().Be("x");
    }

    [Fact]
    public void Join_Filter_Should_Join_Enumerable()
    {
        var result =
            BuiltinFilters.ApplyFilter(BuiltinFilters.JoinFilter, new[] { "a", "b", "c" }, new object[] { "-" });
        result.Should().Be("a-b-c");
    }

    [Fact]
    public void Length_Filter_Should_Return_Length()
    {
        var result = BuiltinFilters.ApplyFilter(BuiltinFilters.LengthFilter, "hello", Array.Empty<object>());
        result.Should().Be(5);
    }

    [Fact]
    public void Lower_Filter_Should_Lowercase_String()
    {
        var result = BuiltinFilters.ApplyFilter(BuiltinFilters.LowerFilter, "HELLO", Array.Empty<object>());
        result.Should().Be("hello");
    }

    [Fact]
    public void Upper_Filter_Should_Uppercase_String()
    {
        var result = BuiltinFilters.ApplyFilter(BuiltinFilters.UpperFilter, "hello", Array.Empty<object>());
        result.Should().Be("HELLO");
    }
}