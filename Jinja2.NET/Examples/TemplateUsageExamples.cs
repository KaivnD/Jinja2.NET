using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;
using System.Text;

namespace Jinja2.NET.Examples;

public static class TemplateUsageExamples
{
    public static void BasicUsage()
    {
        var template = Template.Create("Hello {{ name }}!");
        var result = template.Render(("name", "World"));
    }

    public static void CustomParserUsage()
    {
        var template = Template.CreateWithCustomParser(
            "{% mycustom %}Hello{% endmycustom %}",
            builder => builder.RegisterTag("mycustom", () => new MyCustomTagParser())
        );
    }

    public static void SafeCreation()
    {
        if (Template.TryCreate("{% invalid syntax %}", out var template, out var error))
        {
            var result = template.Render();
        }
        else
        {
            Console.WriteLine($"Template creation failed: {error.Message}");
            Console.WriteLine($"Tokens available: {error.Tokens?.Count ?? 0}");
        }
    }

    public static void AdvancedUsage()
    {
        var template = Template.CreateCustom(
            "Hello {{ name | upper }}!",
            rendererFactory: ctx => new MyCustomRenderer(ctx),
            configureParser: builder => builder
                .RegisterTag("include", () => new IncludeTagParser())
                .WithUtilities()
        );

        template.RegisterFilter("upper", (value, args) => value?.ToString()?.ToUpper());

        var result = template.Render(("name", "world"));

        // Debug info
        template.PrintTokens();
        template.PrintAst();
    }

    public static void FlexibleRendering()
    {
        var template = Template.Create("Hello {{ name }}!");

        // Different ways to render
        var result1 = template.Render(new { name = "World" });
        var result2 = template.Render(new Dictionary<string, object> { ["name"] = "World" });
        var result3 = template.Render(("name", "World"));

        var ctx = new TemplateContext();
        ctx.Set("name", "World");
        var result4 = template.Render(ctx);
    }

    public class IncludeTagParser:ITagParser
    {
        // Implementation for include tag
        public ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    // Example custom classes (you'd implement these)
    public class MyCustomTagParser : ITagParser
    {
        // Implementation depends on your tag parser interface
        public ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class MyCustomRenderer : IRenderer
    {
        private readonly TemplateContext _context;

        public MyCustomRenderer(TemplateContext context)
        {
            _context = context;
        }

        public IVariableContext Context => _context;

        public IReadOnlyDictionary<string, Func<object, object[], object>> CustomFilters { get; }
            = new Dictionary<string, Func<object, object[], object>>().AsReadOnly();

        public IScopeManager ScopeManager { get; } = new ScopeManager();

        public object Visit(ASTNode node)
        {
            // Custom rendering logic - this is just an example
            return node switch
            {
                TextNode textNode => textNode.Content,
                VariableNode variableNode => Context.Get(variableNode.Expression.ToString() ?? ""),
                TemplateNode templateNode => RenderTemplate(templateNode),
                _ => node.ToString()
            };
        }

        public string Render(TemplateNode node)
        {
            var result = new StringBuilder();
            foreach (var child in node.Children)
            {
                var childResult = Visit(child);
                if (childResult != null)
                {
                    result.Append(childResult);
                }
            }
            return result.ToString();
        }

        private string RenderTemplate(TemplateNode templateNode)
        {
            var result = new StringBuilder();
            foreach (var child in templateNode.Children)
            {
                var childResult = Visit(child);
                if (childResult != null)
                {
                    result.Append(childResult);
                }
            }
            return result.ToString();
        }
    }

}