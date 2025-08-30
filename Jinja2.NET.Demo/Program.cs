namespace Jinja2.NET.Demo;
// Token types for the lexer

// Token class

// AST Node base class

// Template node contains the entire template

// Text node for literal text

// Variable node for {{ variable }}

// Block nodes for {% block_name %}

// Expression nodes

// Visitor interface for traversing the AST

// Lexer - tokenizes the template

// Parser - builds AST from tokens

// Built-in filters

// Template context for variable resolution

// Renderer - evaluates the AST and produces output

// Main Jinja2 template class

// Environment class for template loading and caching

// Example usage and tests
public class Program
{
    public static void Main()
    {
        RunExamples();
    }

    public static void RunExamples()
    {
        Console.WriteLine("=== Jinja2.NET Examples ===\n");

        // Basic variable substitution
        Console.WriteLine("1. Basic Variables:");
        var template1 = new Template("Hello {{ name }}! You are {{ age }} years old.");
        var result1 = template1.Render(new { name = "Alice", age = 30 });
        Console.WriteLine(result1);
        Console.WriteLine();

        // Filters
        Console.WriteLine("2. Filters:");
        var template2 = new Template("{{ message | upper }} and {{ message | lower }}");
        var result2 = template2.Render(new { message = "Hello World" });
        Console.WriteLine(result2);
        Console.WriteLine();

        // Attribute access
        Console.WriteLine("3. Attribute Access:");
        var template3 = new Template("User: {{ user.name }} ({{ user.email }})");
        var result3 = template3.Render(new
        {
            user = new { name = "Bob", email = "bob@example.com" }
        });
        Console.WriteLine(result3);
        Console.WriteLine();

        // Multiple filters
        Console.WriteLine("4. Chained Filters:");
        var template4 = new Template("{{ text | lower | capitalize }}");
        var result4 = template4.Render(new { text = "HELLO WORLD" });
        Console.WriteLine(result4);
        Console.WriteLine();

        // Dictionary context
        Console.WriteLine("5. Dictionary Context:");
        var template5 = new Template("Items: {{ items | join(', ') }}");
        var result5 = template5.Render(new Dictionary<string, object>
        {
            ["items"] = new[] { "apple", "banana", "cherry" }
        });
        Console.WriteLine(result5);
        Console.WriteLine();

        Console.WriteLine("=== All examples completed ===");
    }
}