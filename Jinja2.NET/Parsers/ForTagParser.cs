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
        bool isRecursive, ExpressionNode? condition = null)
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

        // Optional 'if' condition for filtering within the for loop
        if (condition != null)
        {
            block.Arguments.Add(new IdentifierNode("if"));
            block.Arguments.Add(condition);
        }

        // Add recursive flag AFTER the iterable/condition
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

    private void ConsumeEndFor(TokenIterator tokens, BlockNode block)
    {
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockStart)
        {
            var next = tokens.Peek(1);
            if (next.Type == ETokenType.Identifier && next.Value.Equals("endfor", StringComparison.OrdinalIgnoreCase))
            {
                var startToken = tokens.Consume(ETokenType.BlockStart);
                tokens.Consume(ETokenType.Identifier); // endfor
                var endToken = tokens.Consume(ETokenType.BlockEnd);

                block.TrimBodyRight = startToken.TrimLeft;
                block.TrimRight = endToken.TrimRight;
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
        ConsumeEndFor(tokens, block);
    }

    private void ParseForSyntax(TokenIterator tokens, IExpressionParser expressionParser, BlockNode block)
    {
        ConsumeForKeyword(tokens);
        var loopVariables = ParseLoopVariables(tokens);
        ConsumeInKeyword(tokens);
        var iterable = ParseIterable(tokens, expressionParser);

        // Check for optional 'if' filter condition after the iterable
        tokens.SkipWhitespace();
        ExpressionNode? condition = null;
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Identifier &&
            tokens.Peek().Value.Equals("if", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Consume(ETokenType.Identifier); // consume 'if'
            condition = expressionParser.Parse(tokens);
        }

        // Check for optional 'recursive' keyword
        tokens.SkipWhitespace();
        var isRecursive = false;
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Identifier &&
            tokens.Peek().Value.Equals("recursive", StringComparison.OrdinalIgnoreCase))
        {
            tokens.Consume(ETokenType.Identifier); // consume 'recursive'
            isRecursive = true;
        }

        AddArgumentsToBlock(block, loopVariables, iterable, isRecursive, condition);
    }


    private ExpressionNode ParseIterable(TokenIterator tokens, IExpressionParser expressionParser)
    {
        tokens.SkipWhitespace();

        // We need to parse the iterable expression but stop before a top-level 'if' or 'recursive'
        // that belong to the for-syntax (e.g. "for x in items if cond"), and also stop at block end.
        var lookahead = 0;
        var depth = 0;
        while (!tokens.IsAtEnd())
        {
            var tk = tokens.Peek(lookahead);

            if (tk.Type == ETokenType.LeftParen || tk.Type == ETokenType.LeftBracket || tk.Type == ETokenType.LeftBrace)
            {
                depth++;
            }
            else if (tk.Type == ETokenType.RightParen || tk.Type == ETokenType.RightBracket || tk.Type == ETokenType.RightBrace)
            {
                depth = Math.Max(0, depth - 1);
            }

            if (depth == 0 && tk.Type == ETokenType.Identifier &&
                (tk.Value.Equals("if", StringComparison.OrdinalIgnoreCase) || tk.Value.Equals("recursive", StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            if (tk.Type == ETokenType.BlockEnd || tk.Type == ETokenType.BlockStart || tk.Type == ETokenType.EOF)
            {
                break;
            }

            lookahead++;
        }

        // Build a temporary token list representing the iterable expression
        var tempTokens = new List<Token>();
        for (var i = 0; i < lookahead; i++)
        {
            tempTokens.Add(tokens.Peek(i));
        }

        // Ensure there's an EOF token at the end of the slice to satisfy TokenIterator.IsAtEnd checks
        tempTokens.Add(new Token(ETokenType.EOF, string.Empty, tokens.CurrentLocation.Line, tokens.CurrentLocation.Column));

        var tempIter = new TokenIterator(tempTokens);
        var iterable = expressionParser.Parse(tempIter);

        // Consume the tokens we used from the original iterator
        for (var i = 0; i < lookahead; i++)
        {
            tokens.Consume(tokens.Peek().Type);
        }

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
        block.TrimBodyLeft = endToken.TrimRight;
    }

    private static void ThrowUnexpectedTokenException(TokenIterator tokens, string expectedKeyword)
    {
        throw new InvalidOperationException(
            $"Expected '{expectedKeyword}' keyword at {GetLocationString(tokens)}");
    }
}