using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

/// <summary>
///     Parser for Jinja2 'for' loop tags that handles iteration over collections.
///     Supports syntax: {% for item in collection %}...{% else %}...{% endfor %} and {% for item in collection %}...{%
///     endfor %}.
/// </summary>
public class ForTagParser : ITagParser
{
    private const string FOR_KEYWORD = "for";
    private const string IN_KEYWORD = "in";
    private readonly Stack<string> _blockStack = new();

    public ASTNode Parse(
        TokenIterator tokens,
        ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser,
        IBlockBodyParser blockBodyParser,
        SourceLocation tagStartLocation,
        ETokenType tagStartTokenType)
    {
        _blockStack.Push(TemplateConstants.BlockNames.For);

        try
        {
            var blockStartToken = GetBlockStartToken(tokens);
            var block = CreateForBlock(tagStartTokenType);

            ParseForSyntax(tokens, expressionParser, block);
            SetTrimFlags(block, blockStartToken, tokens);

            ParseForBody(tokens, blockBodyParser, block);

            return block;
        }
        finally
        {
            _blockStack.Pop();
        }
    }

    private static void AddArgumentsToBlock(BlockNode block, List<string> loopVariables, ExpressionNode iterable,
        bool isRecursive)
    {
        // Add all loop variables as arguments first
        foreach (var variable in loopVariables)
        {
            block.Arguments.Add(new IdentifierNode(variable));
        }

        // Add "in" keyword
        block.Arguments.Add(new IdentifierNode("in"));

        // Add iterable expression
        block.Arguments.Add(iterable);

        // Add recursive flag AFTER the iterable (this is the key fix)
        if (isRecursive)
        {
            block.Arguments.Add(new IdentifierNode("recursive"));
        }
    }

    //private static void AddArgumentsToBlock(BlockNode block, List<string> loopVariables, ExpressionNode iterable)
    //{
    //    // Add all loop variables as arguments
    //    foreach (var variable in loopVariables)
    //    {
    //        block.Arguments.Add(new IdentifierNode(variable));
    //    }

    //    block.Arguments.Add(new IdentifierNode("in")); // Add the "in" keyword
    //    block.Arguments.Add(iterable);
    //}

    private void ConsumeEndFor(TokenIterator tokens)
    {
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockStart)
        {
            var next = tokens.Peek(1);
            if (next.Type == ETokenType.Identifier && next.Value.Equals("endfor", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Consume(ETokenType.BlockStart);
                tokens.Consume(ETokenType.Identifier); // endfor
                tokens.Consume(ETokenType.BlockEnd);
                return;
            }
        }

        throw new InvalidOperationException("Unclosed 'for' block: expected '{% endfor %}' before end of template.");
    }

    private void ConsumeForKeyword(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        var forToken = tokens.Consume(ETokenType.Identifier);

        if (!IsExpectedKeyword(forToken.Value, FOR_KEYWORD))
        {
            ThrowUnexpectedTokenException(tokens, FOR_KEYWORD);
        }
    }

    private void ConsumeInKeyword(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        var inToken = tokens.Consume(ETokenType.Identifier);

        if (!IsExpectedKeyword(inToken.Value, IN_KEYWORD))
        {
            ThrowUnexpectedTokenException(tokens, IN_KEYWORD);
        }
    }

    private static BlockNode CreateForBlock(ETokenType tagStartTokenType)
    {
        return new BlockNode(TemplateConstants.BlockNames.For)
        {
            StartMarkerType = tagStartTokenType
        };
    }

    private static Token GetBlockStartToken(TokenIterator tokens)
    {
        return tokens.Peek(-1); // The last consumed token should be BlockStart
    }

    private static string GetLocationString(TokenIterator tokens)
    {
        return $"{tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}";
    }

    private static bool IsExpectedKeyword(string actual, string expected)
    {
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private void ParseForBody(TokenIterator tokens, IBlockBodyParser blockBodyParser, BlockNode block)
    {
        // Parse main for-body, stopping at "else" or "endfor"
        var children = blockBodyParser.Parse(tokens, "else", "endfor");
        block.Children.AddRange(children);

        // Parse optional else block
        ParseOptionalElseBlock(tokens, blockBodyParser, block);

        // Expect and consume endfor
        ConsumeEndFor(tokens);
    }

    private void ParseForSyntax(TokenIterator tokens, IExpressionParser expressionParser, BlockNode block)
    {
        ConsumeForKeyword(tokens);
        var loopVariables = ParseLoopVariables(tokens);
        ConsumeInKeyword(tokens);
        var iterable = ParseIterable(tokens, expressionParser);

        // Check for optional 'recursive' keyword
        tokens.SkipWhitespace();
        var isRecursive = false;
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Identifier &&
            tokens.Peek().Value.Equals("recursive", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Consume(ETokenType.Identifier); // consume 'recursive'
            isRecursive = true;
        }

        AddArgumentsToBlock(block, loopVariables, iterable, isRecursive);
    }


    private ExpressionNode ParseIterable(TokenIterator tokens, IExpressionParser expressionParser)
    {
        tokens.SkipWhitespace();

        // Parse the iterable expression, but stop at BlockEnd or if we see 'recursive'
        var iterable = expressionParser.Parse(tokens);

        return iterable ?? throw new InvalidOperationException(
            $"Expected expression node for iterable at {GetLocationString(tokens)}");
    }

    private List<string> ParseLoopVariables(TokenIterator tokens)
    {
        var variables = new List<string>();
        tokens.SkipWhitespace();

        // Parse first variable
        variables.Add(tokens.Consume(ETokenType.Identifier).Value);

        // Parse additional variables separated by commas
        while (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
        {
            tokens.Consume(ETokenType.Comma); // consume comma
            tokens.SkipWhitespace();
            variables.Add(tokens.Consume(ETokenType.Identifier).Value);
        }

        return variables;
    }

    private void ParseOptionalElseBlock(TokenIterator tokens, IBlockBodyParser blockBodyParser, BlockNode block)
    {
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockStart)
        {
            var next = tokens.Peek(1);
            if (next.Type == ETokenType.Identifier && next.Value.Equals("else", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Consume(ETokenType.BlockStart);
                tokens.Consume(ETokenType.Identifier); // else
                tokens.Consume(ETokenType.BlockEnd);

                var elseChildren = blockBodyParser.Parse(tokens, "endfor");
                var elseBlock = new BlockNode("else");
                elseBlock.Children.AddRange(elseChildren);
                block.Children.Add(elseBlock);
            }
        }
    }

    private void SetTrimFlags(BlockNode block, Token blockStartToken, TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        var endToken = tokens.Consume(ETokenType.BlockEnd);

        block.EndMarkerType = endToken.Type;
        block.TrimLeft = blockStartToken.TrimLeft;
        block.TrimRight = endToken.TrimRight;
    }

    private static void ThrowUnexpectedTokenException(TokenIterator tokens, string expectedKeyword)
    {
        throw new InvalidOperationException(
            $"Expected '{expectedKeyword}' keyword at {GetLocationString(tokens)}");
    }
}