using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;
using Jinja2.NET.Parsers;
using static Jinja2.NET.Examples.TemplateUsageExamples;

namespace Jinja2.NET.Examples;

public static class MainParserUsageExamples
{
    public static void BasicUsage()
    {
        var parser = MainParser.CreateDefault();
        var result = parser.Parse("{% if true %}Hello{% endif %}");
        var tokens = parser.GetLastTokens(); // Always available
    }

    public class CachedIncludeParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public static void CustomLexerUsage()
    {
        // Custom lexer that handles different syntax
        var parser = MainParser.CreateWithCustomLexer((source, config) =>
            new MyCustomLexer(source, config));

        var result = parser.Parse("<<% if true %>>Hello<<% endif %>>");
    }

    public class FastConditionalParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class IncludeTagParser : ITagParser
    {
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }


    public static void MinimalParser()
    {
        var parser = new MainParser(configure: builder => builder
            .ClearTags()
            .WithControlFlow()
            .RegisterTag("include", () => new IncludeTagParser()));
    }


    // Example custom parsers (placeholders)
    public class MyCustomConditionalParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class MyCustomExpressionParser : ExpressionParser
    {
        /* Your implementation */
    }

    public class MyCustomLexer : Lexer
    {
        public MyCustomLexer(string source, LexerConfig config) : base(source, config)
        {
        }
    }

    public class MyCustomLoopParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class MyCustomRawParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class MyCustomStatementParser : StatementParser
    {
        public MyCustomStatementParser(ExpressionParser? expressionParser,
            TagParserRegistry? tagRegistry, LexerConfig config)
            : base(expressionParser, tagRegistry, config)
        {
        }
    }

    public class OptimizedConditionalParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public class OptimizedExpressionParser : ExpressionParser
    {
    }


    public class ParallelLoopParser : ITagParser
    {
        /* Your implementation */
        public ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
            IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
        {
            return null;
        }
    }

    public static void ReplaceParsers()
    {
        // Example 1: Replace a single tag parser
        var parser = MainParser.CreateCustom(builder =>
            builder.ReplaceParser("if", () => new MyCustomConditionalParser()));

        // Example 2: Replace multiple parsers at once
        var customParsers = new Dictionary<string, Func<ITagParser>>
        {
            ["if"] = () => new MyCustomConditionalParser(),
            ["for"] = () => new MyCustomLoopParser(),
            ["raw"] = () => new MyCustomRawParser()
        };

        var parser2 = MainParser.CreateCustom(builder =>
            builder.ReplaceParsers(customParsers));

        // Example 3: Conditional replacement
        var parser3 = MainParser.CreateCustom(builder =>
            builder.ReplaceParserIf("if",
                () => new OptimizedConditionalParser(),
                () => System.Environment.ProcessorCount > 4));

        // Example 4: Replace core parsers after construction
        var parser4 = MainParser.CreateDefault();
        parser4.ReplaceExpressionParser(new MyCustomExpressionParser());
        parser4.ReplaceStatementParser(new MyCustomStatementParser(
            parser4.ExpressionParser,
            parser4.TagRegistry,
            parser4.Config));

        // Example 5: Using the builder for core parser replacement
        var parser5 = MainParser.CreateCustom(builder =>
        {
            // This would work once the builder methods are implemented
            // builder.ReplaceExpressionParser(new MyCustomExpressionParser())
            //        .ReplaceStatementParser(new MyCustomStatementParser(...));

            // For now, replace tag parsers
            builder.ReplaceParser("if", () => new MyCustomConditionalParser())
                .ReplaceParser("for", () => new MyCustomLoopParser());
        });

        // Example 6: Safe replacement with error handling
        try
        {
            var parser6 = MainParser.CreateCustom(builder =>
                builder.ReplaceParserSafe("if", () => new MyCustomConditionalParser()));
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid parser replacement: {ex.Message}");
        }

        // Example 7: Replace and verify
        var parser7 = MainParser.CreateCustom(builder =>
        {
            builder.ClearTags()
                .ReplaceParser("custom", () => new MyCustomTagParser());

            var registeredTags = builder.GetRegisteredTags();
            Console.WriteLine($"Registered tags: {string.Join(", ", registeredTags)}");
        });

        // Example 8: Fluent replacement chain
        var parser8 = MainParser.CreateCustom(builder =>
            builder.ClearTags()
                .ReplaceParser("if", () => new FastConditionalParser())
                .ReplaceParser("for", () => new ParallelLoopParser())
                .ReplaceParser("include", () => new CachedIncludeParser())
                .WithUtilities()); // Add back standard utilities

        // Example 9: Runtime parser replacement
        var parser9 = MainParser.CreateDefault();

        var needsOptimization = true;

        // Later, based on some condition...
        if (needsOptimization)
        {
            parser9.ReplaceAllParsers(
                new OptimizedExpressionParser() // Will be recreated automatically
            );
        }
    }


    public static void SafeUsage()
    {
        var parser = MainParser.CreateDefault();

        if (parser.TryParse("{% invalid syntax %}", out var node, out var error))
        {
            // Success
        }
        else
        {
            // Error occurred, but tokens are still available
            var tokens = error?.Tokens;
            var stage = error?.Stage;
            Console.WriteLine($"Error at {stage}: {error?.Message}");
        }
    }

    public static void TokenOnlyUsage()
    {
        var parser = MainParser.CreateDefault();

        // Just tokenize for analysis
        if (parser.TryTokenize("{% if condition %}", out var tokens, out var error))
        {
            foreach (var token in tokens)
            {
                Console.WriteLine($"{token.Type}: {token.Value}");
            }
        }
    }
}