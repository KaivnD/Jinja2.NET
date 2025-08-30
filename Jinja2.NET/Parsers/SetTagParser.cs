using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class SetTagParser : ITagParser
{
    public ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
        IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
    {
        var blockStartToken = GetBlockStartToken(tokens);
        var identifiers = ParseIdentifiers(tokens, tagStartLocation);
        var expression = ParseAssignmentExpression(tokens, expressionParser, tagStartLocation);
        tokens.SkipWhitespace();
        var nextToken = tokens.Peek();
        var isLoopScoped = !tokens.IsAtEnd() && nextToken.Type == ETokenType.Identifier &&
                           nextToken.Value.Equals("loop", StringComparison.OrdinalIgnoreCase);
        if (isLoopScoped)
        {
            tokens.Consume(ETokenType.Identifier); // Consume 'loop'
        }

        var endToken = ConsumeBlockEndToken(tokens);
        var block = CreateSetBlock(identifiers, expression, tagStartTokenType, endToken, blockStartToken);
        if (isLoopScoped)
        {
            block.IsLoopScoped = true; // Mark as loop-scoped
        }

        return block;
    }

    private static Token ConsumeBlockEndToken(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        return tokens.Consume(ETokenType.BlockEnd);
    }

    private static void ConsumeOptionalComma(TokenIterator tokens)
    {
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
        {
            tokens.Consume(ETokenType.Comma);
        }
    }

    private static BlockNode CreateSetBlock(List<IdentifierNode> identifiers, ExpressionNode expression,
        ETokenType tagStartTokenType, Token endToken, Token blockStartToken)
    {
        var arguments = new List<ExpressionNode>(identifiers) { expression };
        return new BlockNode(TemplateConstants.BlockNames.Set, arguments, new List<ASTNode>())
        {
            StartMarkerType = tagStartTokenType,
            EndMarkerType = endToken.Type,
            TrimLeft = blockStartToken.TrimLeft,
            TrimRight = endToken.TrimRight
        };
    }


    private static Token GetBlockStartToken(TokenIterator tokens)
    {
        return tokens.Peek(-1); // The last consumed token should be BlockStart
    }

    private static bool HasMoreIdentifiers(TokenIterator tokens)
    {
        return !tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Identifier;
    }

    private static ExpressionNode ParseAssignmentExpression(TokenIterator tokens, IExpressionParser expressionParser,
        SourceLocation tagStartLocation)
    {
        tokens.SkipWhitespace();
        ValidateEqualsToken(tokens, tagStartLocation);
        tokens.Consume(ETokenType.Equals);
        tokens.SkipWhitespace();

        // Custom parse: stop at BlockEnd or at identifier "loop"
        var exprTokens = new List<Token>();
        while (!tokens.IsAtEnd())
        {
            var t = tokens.Peek();
            if (t.Type == ETokenType.BlockEnd ||
                (t.Type == ETokenType.Identifier && t.Value.Equals("loop", StringComparison.OrdinalIgnoreCase)))
            {
                break;
            }

            exprTokens.Add(tokens.Consume(t.Type));
        }

        // Now parse the collected tokens as an expression
        var exprIterator = new TokenIterator(exprTokens);
        return expressionParser.Parse(exprIterator);
    }

    private static List<IdentifierNode> ParseIdentifiers(TokenIterator tokens, SourceLocation tagStartLocation)
    {
        tokens.SkipWhitespace();
        tokens.Consume(ETokenType.Identifier); // Consumes 'set'
        var identifiers = new List<IdentifierNode>();

        // Only consume identifiers until we see '='
        while (HasMoreIdentifiers(tokens) && tokens.Peek().Type == ETokenType.Identifier)
        {
            // Stop if the next token after this identifier is '='
            if (tokens.Peek(1).Type == ETokenType.Equals)
            {
                break;
            }

            var identifierToken = tokens.Consume(ETokenType.Identifier);
            identifiers.Add(new IdentifierNode(identifierToken.Value));

            tokens.SkipWhitespace();
            ConsumeOptionalComma(tokens);
            tokens.SkipWhitespace();
        }

        // Also handle the last identifier before '='
        if (HasMoreIdentifiers(tokens) && tokens.Peek(1).Type == ETokenType.Equals)
        {
            var identifierToken = tokens.Consume(ETokenType.Identifier);
            identifiers.Add(new IdentifierNode(identifierToken.Value));
        }

        ValidateIdentifiersExist(identifiers, tagStartLocation);
        return identifiers;
    }

    private static void ValidateEqualsToken(TokenIterator tokens, SourceLocation tagStartLocation)
    {
        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.Equals)
        {
            throw new TemplateParsingException(
                $"Expected '=' in set block at {tagStartLocation.Line}:{tagStartLocation.Column}");
        }
    }

    private static void ValidateIdentifiersExist(List<IdentifierNode> identifiers, SourceLocation tagStartLocation)
    {
        if (identifiers.Count == 0)
        {
            throw new TemplateParsingException(
                $"Expected at least one identifier in set block at {tagStartLocation.Line}:{tagStartLocation.Column}");
        }
    }
}