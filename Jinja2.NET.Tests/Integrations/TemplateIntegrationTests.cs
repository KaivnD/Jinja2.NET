using System.Text.RegularExpressions;
using FluentAssertions;

namespace Jinja2.NET.Tests.Integrations;

public class TemplateIntegrationTests
{
  [Fact]
  public void Should_Render_Attribute_Access()
  {
    // Arrange
    var template = new Template("User: {{ user.name }} ({{ user.email }})");

    // Act
    var result = template.Render(new
    {
      user = new { name = "Bob", email = "bob@example.com" }
    });

    // Assert
    result.Should().Be("User: Bob (bob@example.com)");
  }

  [Fact]
  public void Should_Render_Basic_Variables()
  {
    // Arrange
    var template = new Template("Hello {{ name }}! You are {{ age }} years old.");

    // Act
    var result = template.Render(new { name = "Alice", age = 30 });

    // Assert
    result.Should().Be("Hello Alice! You are 30 years old.");
  }

  [Fact]
  public void Should_Render_Chained_Filters()
  {
    // Arrange
    var template = new Template("{{ text | lower | capitalize }}");

    // Act
    var result = template.Render(new { text = "HELLO WORLD" });

    // Assert
    result.Should().Be("Hello world");
  }

  [Fact]
  public void Should_Render_Dictionary_Context()
  {
    // Arrange
    var template = new Template("Items: {{ items | join(', ') }}");

    // Create a simple array with known values
    var items = new[] { "apple", "banana", "cherry" };

    // Act
    var result = template.Render(new Dictionary<string, object>
    {
      ["items"] = items
    });

    // Verify the input is correct
    items.Should().NotBeNull().And.HaveCount(3);
    items[0].Should().Be("apple");

    // Assert
    result.Should().Be("Items: apple, banana, cherry");

    // If test fails, output debugging information
    if (result != "Items: apple, banana, cherry")
    {
      // Output what the actual result was for debugging
      Console.WriteLine("Expected: 'Items: apple, banana, cherry'");
      Console.WriteLine($"Actual: '{result}'");
      Console.WriteLine($"Char at index 7: '{(result.Length > 7 ? result[7] : ' ')}'");
    }
  }

  [Fact]
  public void Should_Render_For_Loop()
  {
    // Arrange
    var expected = "- a\n- b\n- c";
    //var template = new Template(@"{% for item in items %}- {{ item }}\n{% endfor %}");
    var template = new Template(
      @"{% for item in items %}- {{ item }}
{% endfor %}");
    // Act
    var render = template.Render(new { items = new[] { "a", "b", "c" } });
    var result = render.Trim();

    // Assert
    var replace = result.Replace("\r\n", "\n");
    replace.Should().Be(expected);
  }

  [Fact]
  public void Should_Render_For_Loop_NoNewLines()
  {
    var template = new Template(@"{% for item in items %}- {{ item }}{% endfor %}
");
    var render = template.Render(new { items = new[] { "a", "b", "c" } });
    var result = render.Trim();
    var expected = "- a- b- c";
    var replace = result.Replace("\r\n", "\n");
    replace.Should().Be(expected);
  }

  [Fact]
  public void Should_Render_If_Else_Branch()
  {
    var template = new Template(@"
{% if is_admin %}Welcome, admin.{% else %}Access denied.{% endif %}
");
    var result1 = template.Render(new { is_admin = true }).Trim();
    var result2 = template.Render(new { is_admin = false }).Trim();

    string Normalize(string s)
    {
      return Regex.Replace(s.Replace("\r\n", "\n").Trim(), @"\n+", "\n").Trim();
    }

    Normalize(result1).Should().Be("Welcome, admin.");
    Normalize(result2).Should().Be("Access denied.");
  }

  [Fact]
  public void Should_Render_Set_Assignment()
  {
    var template = new Template(@"
{% set message = ""Hello "" + name %}
{{ message }}
");
    var result = template.Render(new { name = "World" }).Trim();
    result.Should().Be("Hello World");
  }

  [Fact]
  public void Should_Render_With_Filters()
  {
    // Arrange
    var template = new Template("{{ message | upper }} and {{ message | lower }}");

    // Act
    var result = template.Render(new { message = "Hello World" });

    // Assert
    result.Should().Be("HELLO WORLD and hello world");
  }

  [Fact]
  public void Should_Use_Custom_Filter()
  {
    var template = new Template(@"{{ 'hello' | shout }}");
    template.RegisterFilter("shout", (input, args) => input.ToString().ToUpper() + "!");
    var result = template.Render(new { });
    result.Should().Be("HELLO!");
  }
}