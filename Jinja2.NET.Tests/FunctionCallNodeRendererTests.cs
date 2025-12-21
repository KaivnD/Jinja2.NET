using FluentAssertions;
using Xunit;

namespace Jinja2.NET.Tests;

public class FunctionCallNodeRendererTests
{
    [Fact]
    public void Range_Function_Should_Generate_Correct_List()
    {
        var template = new Template("{{ range(3) | join(',') }}");
        var result = template.Render();
        result.Should().Be("0,1,2");

        template = new Template("{{ range(1,5) | join(',') }}");
        result = template.Render();
        result.Should().Be("1,2,3,4");

        template = new Template("{{ range(5,1,-2) | join(',') }}");
        result = template.Render();
        result.Should().Be("5,3");

        // zero args -> empty
        template = new Template("{{ range() | join(',') }}");
        result = template.Render();
        result.Should().Be("");

        // step = 0 should throw
        template = new Template("{{ range(1,5,0) | join(',') }}");
        Action act = () => template.Render();
        act.Should().Throw<ArgumentException>().WithMessage("*step argument must not be zero*");
    }

    [Fact]
    public void Namespace_Function_Should_Merge_Dictionary_And_Keywords()
    {
        var template = new Template("{{ namespace(mydict).a }}|{{ namespace(mydict).b }}");
        var data = new Dictionary<string, object>
        {
            ["mydict"] = new Dictionary<string, object>
            {
                ["a"] = 1,
                ["b"] = "two"
            }
        };

        var result = template.Render(data);
        result.Should().Be("1|two");

        // also test kwargs merging
        template = new Template("{{ namespace(mydict, c=3).c }}");
        result = template.Render(data);
        result.Should().Be("3");

        // namespace with no args returns empty mapping
        template = new Template("{% set ns = namespace() %}{{ ns | length }}");
        result = template.Render();
        result.Should().Be("0");

        // kwargs override existing keys
        template = new Template("{{ namespace(mydict, a=42).a }}");
        result = template.Render(data);
        result.Should().Be("42");

        // source dictionary with non-string key should be stringified
        var data2 = new Dictionary<string, object>
        {
            ["mydict"] = new Dictionary<object, object>
            {
                [1] = "one",
                ["x"] = "ex"
            }
        };
        template = new Template("{{ namespace(mydict)[\"1\"] }}|{{ namespace(mydict).x }}");
        result = template.Render(data2);
        // key '1' becomes "1" when converted
        result.Should().Be("one|ex");
    }
}
