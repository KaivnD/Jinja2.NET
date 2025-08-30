using Jinja2.NET.Nodes;
using Jinja2.NET.Parsers;

namespace Jinja2.NET;

public class MainParser
{
    private readonly LexerConfig _config;
    protected readonly TagParserRegistry _tagRegistry;
    protected ExpressionParser _expressionParser;
    private List<Token> _lastTokens;
    protected StatementParser _statementParser;
    public LexerConfig Config => _config;
    public ExpressionParser ExpressionParser => _expressionParser;
    public Lexer LastLexer { get; private set; }

    public Func<string, LexerConfig, Lexer> LexerFactory { get; set; }
    public StatementParser StatementParser => _statementParser;

    // Expose all components for advanced access
    public TagParserRegistry TagRegistry => _tagRegistry;
    public TemplateBodyParser TemplateBodyParser { get; private set; }

    public MainParser(LexerConfig config = null, Action<MainParserBuilder> configure = null)
    {
        _config = config ?? new LexerConfig();
        _tagRegistry = new TagParserRegistry();
        InitializeParsers();

        LexerFactory = (source, lexerConfig) => new Lexer(source, lexerConfig);

        var builder = new MainParserBuilder(this);
        ConfigureDefaults(builder);
        configure?.Invoke(builder);
    }

    public static MainParser CreateCustom(Action<MainParserBuilder> configure)
    {
        return new MainParser(configure: configure);
    }

    // Factory methods
    public static MainParser CreateDefault()
    {
        return new MainParser();
    }

    public static MainParser CreateMinimal()
    {
        return new MainParser(configure: builder => builder.ClearTags());
    }

    public static MainParser CreateWithCustomLexer(Func<string, LexerConfig, Lexer> lexerFactory)
    {
        return new MainParser(configure: builder => builder.WithLexerFactory(lexerFactory));
    }

    // Rest of your existing methods remain unchanged...
    public IReadOnlyList<Token> GetLastTokens()
    {
        return _lastTokens ?? new List<Token>();
    }

    public bool HasTokens()
    {
        return _lastTokens != null && _lastTokens.Count > 0;
    }

    public virtual TemplateNode Parse(string source)
    {
        IReadOnlyList<Token> tokens;

        try
        {
            tokens = TokenizeOnly(source);
        }
        catch (Exception ex)
        {
            throw new TemplateParsingException($"Tokenization failed: {ex.Message}", ex)
            {
                Source = source,
                Tokens = _lastTokens?.AsReadOnly(),
                Stage = EParsingStage.Tokenization
            };
        }

        try
        {
            var tokenIterator = new TokenIterator(_lastTokens);
            return TemplateBodyParser.Parse(tokenIterator);
        }
        catch (Exception ex)
        {
            throw new TemplateParsingException($"Parsing failed: {ex.Message}", ex)
            {
                Source = source,
                Tokens = _lastTokens?.AsReadOnly(),
                Stage = EParsingStage.Parsing
            };
        }
    }

    public (TemplateNode Node, IReadOnlyList<Token> Tokens) ParseWithTokens(string source)
    {
        var node = Parse(source);
        return (node, _lastTokens);
    }

    // Factory method for creating parser with custom dependencies
    public void ReplaceAllParsers(
        ExpressionParser expressionParser = null,
        StatementParser statementParser = null,
        TemplateBodyParser templateBodyParser = null)
    {
        if (expressionParser != null)
        {
            _expressionParser = expressionParser;
        }

        if (statementParser != null)
        {
            _statementParser = statementParser;
        }
        else if (expressionParser != null)
        {
            // Recreate statement parser if expression parser changed
            _statementParser = new StatementParser(_expressionParser, _tagRegistry, _config);
        }

        if (templateBodyParser != null)
        {
            TemplateBodyParser = templateBodyParser;
        }
        else if (statementParser != null || expressionParser != null)
        {
            // Recreate template body parser if statement parser changed
            TemplateBodyParser = new TemplateBodyParser(_statementParser);
        }
    }

    // Parser replacement methods - the key addition for clean replacement
    public void ReplaceExpressionParser(ExpressionParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        _expressionParser = newParser;

        // Recreate dependent parsers with new expression parser
        _statementParser = new StatementParser(_expressionParser, _tagRegistry, _config);
        TemplateBodyParser = new TemplateBodyParser(_statementParser);
    }

    public void ReplaceStatementParser(StatementParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        _statementParser = newParser;

        // Recreate dependent parsers
        TemplateBodyParser = new TemplateBodyParser(_statementParser);
    }

    public void ReplaceTemplateBodyParser(TemplateBodyParser newParser)
    {
        if (newParser == null)
        {
            throw new ArgumentNullException(nameof(newParser));
        }

        TemplateBodyParser = newParser;
    }

    public IReadOnlyList<Token> TokenizeOnly(string source)
    {
        try
        {
            LastLexer = LexerFactory(source, _config);
            var tokens = LastLexer.Tokenize();
            _lastTokens = tokens.ToList();
            return _lastTokens;
        }
        catch
        {
            _lastTokens = LastLexer?.GetPartialTokens()?.ToList() ?? new List<Token>();
            throw;
        }
    }

    public bool TryParse(string source, out TemplateNode node, out TemplateParsingException error)
    {
        try
        {
            node = Parse(source);
            error = null;
            return true;
        }
        catch (TemplateParsingException ex)
        {
            node = null;
            error = ex;
            return false;
        }
    }

    public bool TryTokenize(string source, out IReadOnlyList<Token> tokens, out Exception error)
    {
        try
        {
            tokens = TokenizeOnly(source);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            tokens = _lastTokens ?? new List<Token>();
            error = ex;
            return false;
        }
    }

    private void ConfigureDefaults(MainParserBuilder builder)
    {
        builder
            .RegisterTag(TemplateConstants.BlockNames.If, () => new ConditionalBlockParser())
            .RegisterTag(TemplateConstants.BlockNames.For, () => new ForTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Raw, () => new RawTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Set, () => new SetTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Elif, () => new ElifTagParser())
            .RegisterTag(TemplateConstants.BlockNames.Else, () => new ElseTagParser())
            .RegisterTag(TemplateConstants.BlockNames.EndIf, () => new EndTagParser(TemplateConstants.BlockNames.EndIf))
            .RegisterTag(TemplateConstants.BlockNames.EndFor,
                () => new EndTagParser(TemplateConstants.BlockNames.EndFor))
            .RegisterTag(TemplateConstants.BlockNames.EndRaw,
                () => new EndTagParser(TemplateConstants.BlockNames.EndRaw));
    }

    private void InitializeParsers()
    {
        _expressionParser = new ExpressionParser();
        _statementParser = new StatementParser(_expressionParser, _tagRegistry, _config);
        TemplateBodyParser = new TemplateBodyParser(_statementParser);
    }
}