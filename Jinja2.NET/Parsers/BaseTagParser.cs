using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

/// <summary>
///     Base class for shared parser logic
/// </summary>
public abstract class BaseTagParser : ITagParser
{
    public abstract ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser, IBlockBodyParser blockBodyParser,
        SourceLocation tagStartLocation, ETokenType tagStartTokenType);

    protected void ConfigureBlockNode(BlockNode block, ExpressionNode? condition, Token blockStartToken, Token endToken)
    {
        if (condition != null)
        {
            block.Arguments.Add(condition);
        }

        block.EndMarkerType = endToken.Type;
        block.TrimLeft = blockStartToken.TrimLeft;
        block.TrimRight = endToken.TrimRight;
    }

    protected Token ConsumeBlockEnd(TokenIterator tokens)
    {
        return tokens.Consume(ETokenType.BlockEnd);
    }

    protected BlockNode CreateBlockNode(string tagName, ETokenType startMarkerType)
    {
        return new BlockNode(tagName) { StartMarkerType = startMarkerType };
    }

    protected InvalidOperationException CreateParseException(string message, SourceLocation location)
    {
        return new InvalidOperationException($"{message} at {location.Line}:{location.Column}");
    }

    protected void ParseBlockBody(TokenIterator tokens, IBlockBodyParser blockBodyParser, BlockNode block,
        params string[] terminators)
    {
        block.Children.AddRange(blockBodyParser.Parse(tokens, terminators));

        // Check for correct end tag or valid continuation
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockStart)
        {
            // Look ahead to the identifier after BlockStart
            var lookahead = tokens.Peek(1);
            if (lookahead.Type == ETokenType.Identifier)
            {
                var found = lookahead.Value.ToLowerInvariant();
                // Accept any of the terminators (elif, else, endif)
                if (terminators.Length > 0 &&
                    !terminators.Any(t => string.Equals(t, found, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"Expected one of [{string.Join(", ", terminators)}] block, but found '{found}' at {lookahead.Line}:{lookahead.Column}");
                }
            }
        }
    }

    protected void SkipWhitespace(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
    }
}