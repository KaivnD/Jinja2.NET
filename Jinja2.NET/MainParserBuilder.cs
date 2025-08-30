using Jinja2.NET.Interfaces;
using Jinja2.NET.Parsers;

namespace Jinja2.NET;

public class MainParserBuilder
{
    private readonly MainParser _parser;

    internal MainParserBuilder(MainParser parser)
    {
        _parser = parser;
    }

    // Existing methods with potential enhancements
    public MainParserBuilder ClearTags()
    {
        _parser.TagRegistry.ClearParsers();
        return this;
    }

    // Method to check what parsers are currently registered
    public IEnumerable<string> GetRegisteredTags()
    {
        // This assumes TagParserRegistry has a method to list registered tags
        // You might need to implement this in TagParserRegistry
        return _parser.TagRegistry.GetRegisteredTagNames();
    }

    public MainParserBuilder RegisterTag<T>(string tagName, Func<T> parserFactory) where T : ITagParser
    {
        var parser = parserFactory();
        _parser.TagRegistry.RegisterParser(tagName, parser);
        return this;
    }

    // Batch operations for cleaner replacement scenarios
    public MainParserBuilder ReplaceDefaultParsers()
    {
        return ClearTags()
            .WithControlFlow()
            .WithLoops()
            .WithUtilities();
    }

    // Method 5: Replace core parsers with dedicated methods
    public MainParserBuilder ReplaceExpressionParser(ExpressionParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        _parser.ReplaceExpressionParser(newParser);
        return this;
    }

    // Method 1: Direct replacement - cleanest approach
    public MainParserBuilder ReplaceParser<T>(string tagName, Func<T> parserFactory) where T : ITagParser
    {
        // Remove existing parser if it exists
        if (_parser.TagRegistry.HasParser(tagName))
        {
            _parser.TagRegistry.UnregisterParser(tagName);
        }

        // Register the new parser
        var parser = parserFactory();
        _parser.TagRegistry.RegisterParser(tagName, parser);
        return this;
    }

    // Method 3: Conditional replacement
    public MainParserBuilder ReplaceParserIf<T>(string tagName, Func<T> parserFactory,
        Func<bool> condition) where T : ITagParser
    {
        if (condition())
        {
            return ReplaceParser(tagName, parserFactory);
        }

        return this;
    }

    // Method 4: Replace multiple parsers at once
    public MainParserBuilder ReplaceParsers(Dictionary<string, Func<ITagParser>> parserFactories)
    {
        foreach (var kvp in parserFactories)
        {
            ReplaceParser(kvp.Key, kvp.Value);
        }

        return this;
    }

    // Method 2: Fluent replacement with validation
    public MainParserBuilder ReplaceParserSafe<T>(string tagName, Func<T> parserFactory) where T : ITagParser
    {
        if (string.IsNullOrEmpty(tagName))
        {
            throw new ArgumentException("Tag name cannot be null or empty", nameof(tagName));
        }

        if (parserFactory == null)
        {
            throw new ArgumentNullException(nameof(parserFactory));
        }

        // Always replace - unregister first, then register
        _parser.TagRegistry.UnregisterParser(tagName); // Should handle non-existent gracefully

        var parser = parserFactory();
        _parser.TagRegistry.RegisterParser(tagName, parser);
        return this;
    }

    public MainParserBuilder ReplaceStatementParser(StatementParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        _parser.ReplaceStatementParser(newParser);
        return this;
    }

    public MainParserBuilder ReplaceTemplateBodyParser(TemplateBodyParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        _parser.ReplaceTemplateBodyParser(newParser);
        return this;
    }

    public MainParserBuilder UnregisterTag(string tagName)
    {
        if (_parser.TagRegistry.HasParser(tagName))
        {
            _parser.TagRegistry.UnregisterParser(tagName);
        }

        return this;
    }

    public MainParserBuilder WithControlFlow()
    {
        return RegisterTag(TemplateConstants.BlockNames.If, () => new ConditionalBlockParser())
            .RegisterTag(TemplateConstants.BlockNames.Elif, () => new ElifTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Else, () => new ElseTagParser())
            .RegisterTag(TemplateConstants.BlockNames.EndIf,
                () => new EndTagParser(TemplateConstants.BlockNames.EndIf));
    }

    // Enhanced helper methods that support replacement
    public MainParserBuilder WithCustomControlFlow<TIf, TElif, TElse, TEndIf>()
        where TIf : ITagParser, new()
        where TElif : ITagParser, new()
        where TElse : ITagParser, new()
        where TEndIf : ITagParser, new()
    {
        return ReplaceParser(TemplateConstants.BlockNames.If, () => new TIf())
            .ReplaceParser(TemplateConstants.BlockNames.Elif, () => new TElif())
            .ReplaceParser(TemplateConstants.BlockNames.Else, () => new TElse())
            .ReplaceParser(TemplateConstants.BlockNames.EndIf, () => new TEndIf());
    }

    public MainParserBuilder WithLexerFactory(Func<string, LexerConfig, Lexer> factory)
    {
        _parser.LexerFactory = factory;
        return this;
    }

    public MainParserBuilder WithLoops()
    {
        return RegisterTag(TemplateConstants.BlockNames.For, () => new ForTagParser())
            .RegisterTag(TemplateConstants.BlockNames.EndFor,
                () => new EndTagParser(TemplateConstants.BlockNames.EndFor));
    }

    public MainParserBuilder WithUtilities()
    {
        return RegisterTag(TemplateConstants.BlockNames.Raw, () => new RawTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Set, () => new SetTagParser())
            .RegisterTag(TemplateConstants.BlockNames.EndRaw,
                () => new EndTagParser(TemplateConstants.BlockNames.EndRaw));
    }
}