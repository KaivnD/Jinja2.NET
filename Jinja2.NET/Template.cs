using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET;

/// <summary>
///     Represents a flexible compiled Jinja2 template that can be rendered with different contexts.
///     Supports custom parsers, renderers, and extensive configuration options.
/// </summary>
public class Template
{
    private readonly TemplateNode _ast;

    private readonly Dictionary<string, Func<object, object[], object>> _customFilters =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly MainParser _parser;
    private readonly Func<TemplateContext, IRenderer> _rendererFactory;
    private readonly string _source;
    private readonly IReadOnlyList<Token> _tokens;

    // Properties for debugging and introspection
    public TemplateNode Ast => _ast;
    public IReadOnlyDictionary<string, Func<object, object[], object>> CustomFilters => _customFilters.AsReadOnly();
    public MainParser Parser => _parser;
    public string Source => _source;
    public IReadOnlyList<Token> Tokens => _tokens;

    // Main constructor with full flexibility
    public Template(string source,
        LexerConfig? config,
        Func<TemplateContext, IRenderer>? rendererFactory = null,
        MainParser? customParser = null,
        Action<MainParserBuilder>? configureParser = null)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));

        // Use custom parser or create new one
        _parser = customParser ?? new MainParser(config, configureParser);

        // Set default renderer factory
        //_rendererFactory = rendererFactory ?? (context => new Renderer(context, _customFilters));
        _rendererFactory = rendererFactory ?? (context => new Renderer(context, new ScopeManager(), _customFilters));

        // Parse and store results
        var result = _parser.ParseWithTokens(source);
        if (result.Node != null)
        {
            _ast = result.Node;
        }

        _tokens = result.Tokens;
    }

    // Convenience constructor for simple cases
    public Template(string source, LexerConfig? config = null)
        : this(source, config, null)
    {
    }

    // Factory methods for common scenarios
    public static Template Create(string source)
    {
        return new Template(source);
    }

    public static Template CreateCustom(string source,
        LexerConfig? config = null,
        Func<TemplateContext, IRenderer>? rendererFactory = null,
        Action<MainParserBuilder>? configureParser = null)
    {
        return new Template(source, config, rendererFactory, null, configureParser);
    }

    public static Template CreateMinimal(string source)
    {
        return new Template(source, null, configureParser: builder => builder.ClearTags().WithControlFlow());
    }

    public static Template CreateWithCustomParser(string source, Action<MainParserBuilder> configureParser)
    {
        return new Template(source, null, configureParser: configureParser);
    }

    public IEnumerable<string> GetFilterNames()
    {
        // You'd implement this to analyze the AST for filter usage
        return new List<string>();
    }

    public IEnumerable<string> GetTagNames()
    {
        // You'd implement this to analyze the AST for tag usage
        return new List<string>();
    }

    // Template analysis methods
    public IEnumerable<string> GetVariableNames()
    {
        // You'd implement this to analyze the AST for variable references
        // This is a placeholder - actual implementation would traverse the AST
        return new List<string>();
    }

    public bool HasFilter(string name)
    {
        return _customFilters.ContainsKey(name);
    }

    public void PrintAst()
    {
        Console.WriteLine($"AST for template: {_source}");
        Console.WriteLine(_ast.ToString());
    }

    // Debugging helpers
    public void PrintTokens()
    {
        Console.WriteLine($"Tokens for template: {_source}");
        for (var i = 0; i < _tokens.Count; i++)
        {
            var token = _tokens[i];
            Console.WriteLine($"  {i}: {token.Type} = '{token.Value}'");
        }
    }

    public void RegisterFilter(string name, Func<object, object[], object> filter)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Filter name cannot be null or whitespace.", nameof(name));
        }

        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        _customFilters[name] = filter;
    }


    public string Render(object? context = null, bool reuseContext = false)
    {
        TemplateContext templateContext;
        if (reuseContext && context is TemplateContext ctx)
        {
            templateContext = ctx;
        }
        else
        {
            templateContext = new TemplateContext();
            if (context != null)
            {
                // Handle different context types
                switch (context)
                {
                    case Dictionary<string, object> dict:
                        templateContext.SetAll(dict);
                        break;
                    case TemplateContext tc:
                        //// Copy from another TemplateContext
                        //var contextDict = new Dictionary<string, object>();
                        //// You'd need to implement a way to extract variables from TemplateContext
                        //templateContext.SetAll(contextDict);

                        templateContext = tc;
                        break;
                    default:
                        // Handle anonymous objects and other types using reflection
                        var properties = context.GetType().GetProperties();
                        foreach (var prop in properties)
                        {
                            templateContext.Set(prop.Name, prop.GetValue(context));
                        }

                        break;
                }
            }
        }

        var renderer = _rendererFactory(templateContext);

        if (renderer is Renderer concreteRenderer)
        {
            return concreteRenderer.Render(_ast);
        }

        var result = new StringBuilder();
        foreach (var child in _ast.Children)
        {
            var childResult = renderer.Visit(child);
            if (childResult != null)
            {
                result.Append(childResult);
            }
        }

        return result.ToString();
    }

    public string Render(Dictionary<string, object> variables)
    {
        var templateContext = new TemplateContext();
        foreach (var kvp in variables)
        {
            templateContext.Set(kvp.Key, kvp.Value);
        }

        var renderer = _rendererFactory(templateContext);
        return renderer.Render(_ast);
    }

    // Additional render overloads for flexibility
    public string Render(TemplateContext context)
    {
        var renderer = _rendererFactory(context);
        return renderer.Render(_ast);
    }

    public string Render(params (string key, object value)[] variables)
    {
        var templateContext = new TemplateContext();
        foreach (var (key, value) in variables)
        {
            templateContext.Set(key, value);
        }

        var renderer = _rendererFactory(templateContext);
        return renderer.Render(_ast);
    }

    // Advanced rendering with custom renderer
    public string RenderWith(IRenderer customRenderer)
    {
        return customRenderer.Render(_ast);
    }

    public override string ToString()
    {
        return $"Template: {_source}";
    }

    // Safe creation with error handling
    public static bool TryCreate(string source, out Template? template, out TemplateParsingException? error,
        LexerConfig? config = null, Action<MainParserBuilder>? configureParser = null)
    {
        try
        {
            template = new Template(source, config, configureParser: configureParser);
            error = null;
            return true;
        }
        catch (TemplateParsingException ex)
        {
            template = null;
            error = ex;
            return false;
        }
    }

    public void UnregisterFilter(string name)
    {
        _customFilters.Remove(name);
    }

    // Clone template with modifications
    public Template WithCustomFilters(Dictionary<string, Func<object, object[], object>> filters)
    {
        var newTemplate = new Template(_source, _parser.Config, _rendererFactory, _parser);
        foreach (var filter in _customFilters)
        {
            newTemplate._customFilters[filter.Key] = filter.Value;
        }

        foreach (var filter in filters)
        {
            newTemplate._customFilters[filter.Key] = filter.Value;
        }

        return newTemplate;
    }

    public Template WithRenderer(Func<TemplateContext, IRenderer> rendererFactory)
    {
        return new Template(_source, _parser.Config, rendererFactory, _parser);
    }
}
