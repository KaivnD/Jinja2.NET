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
using Jinja2.NET;
using Jinja2.NET.Nodes;

public class Program
{
    public static void Main(string[] args)
    {
        if (args != null && args.Length >= 2 && args[0] == "dump-tokens")
        {
            DumpTokensAndAst(args[1]);
            return;
        }

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

    private static void DumpTokensAndAst(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return;
        }

        var source = System.IO.File.ReadAllText(path);
        var parser = new MainParser();
        try
        {
            // First tokenize so we can always inspect tokens even if parsing fails
            var tokens = parser.TokenizeOnly(source);

            Console.WriteLine("Tokens:");
            var max = tokens.Count;
            for (int i = 0; i < max; i++)
            {
                var t = tokens[i];
                Console.WriteLine($"{i:000}: {t.Type} '{t.Value.Replace("\n","\\n")} ' @ {t.Line}:{t.Column} (TrimLeft={t.TrimLeft}, TrimRight={t.TrimRight})");
            }

            Console.WriteLine();
            Console.WriteLine("AST Summary:");
            try
            {
                var (node, parsedTokens) = parser.ParseWithTokens(source);
                if (node == null)
                {
                    Console.WriteLine("(no AST produced)");
                }
                else
                {
                    DumpNode(node, 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parse failed: " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Parse failed: " + ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    private static void DumpNode(object? node, int indent)
    {
        var pad = new string(' ', indent * 2);
        if (node == null)
        {
            Console.WriteLine(pad + "(null)");
            return;
        }

        switch (node)
        {
            case TemplateNode t:
                Console.WriteLine(pad + "TemplateNode (children=" + t.Children.Count + ")");
                foreach (var c in t.Children) DumpNode(c, indent + 1);
                break;
            case BlockNode b:
                Console.WriteLine(pad + $"BlockNode name={b.Name} args={b.Arguments.Count} children={b.Children.Count}");
                foreach (var a in b.Arguments) Console.WriteLine(pad + "  Arg: " + a.GetType().Name);
                foreach (var c in b.Children) DumpNode(c, indent + 1);
                break;
            case TextNode tn:
                var preview = tn.Content.Length > 60 ? tn.Content.Substring(0, 60).Replace("\n","\\n") + "..." : tn.Content.Replace("\n","\\n");
                Console.WriteLine(pad + $"TextNode: '{preview}' (TrimLeft={tn.TrimLeft},TrimRight={tn.TrimRight})");
                break;
            default:
                Console.WriteLine(pad + node.GetType().Name);
                // Try to reflect children properties
                var props = node.GetType().GetProperties();
                foreach (var p in props)
                {
                    if (p.PropertyType == typeof(List<ASTNode>))
                    {
                        var list = p.GetValue(node) as System.Collections.IEnumerable;
                        if (list != null)
                        {
                            int count = 0;
                            foreach (var _ in list) count++;
                            Console.WriteLine(pad + $"  {p.Name}: list({count})");
                        }
                    }
                }
                break;
        }
    }
}
 