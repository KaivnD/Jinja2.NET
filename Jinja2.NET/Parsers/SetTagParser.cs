using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class SetTagParser : BaseTagParser
{
    public override ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser,
        IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType)
    {
        var blockStartToken = GetBlockStartToken(tokens);
        var targets = ParseTargets(tokens, expressionParser);
        // After targets, we can have either an assignment form: "set a = expr"
        // or a block form: "set name %} ... {% endset %}". Detect which one and handle.
        tokens.SkipWhitespace();
        // If next token is BlockEnd -> block form
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockEnd)
        {
            var endToken = ConsumeBlockEndToken(tokens);
            var block = CreateSetBlock(targets, new LiteralNode(string.Empty), tagStartTokenType, endToken, blockStartToken);
            // Parse body until endset
            ParseBlockBody(tokens, blockBodyParser, block, TemplateConstants.BlockNames.EndSet);
            return block;
        }

        // Otherwise it's assignment form
        var expression = ParseAssignmentExpression(tokens, expressionParser, tagStartLocation);
        tokens.SkipWhitespace();
        var nextToken = tokens.Peek();
        var isLoopScoped = !tokens.IsAtEnd() && nextToken.Type == ETokenType.Identifier &&
                           nextToken.Value.Equals("loop", StringComparison.OrdinalIgnoreCase);
        if (isLoopScoped)
        {
            tokens.Consume(ETokenType.Identifier); // Consume 'loop'
        }

        var endTok = ConsumeBlockEndToken(tokens);
        var assignBlock = CreateSetBlock(targets, expression, tagStartTokenType, endTok, blockStartToken);
        if (isLoopScoped)
        {
            assignBlock.IsLoopScoped = true; // Mark as loop-scoped
        }

        return assignBlock;
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

    private static BlockNode CreateSetBlock(List<ExpressionNode> targets, ExpressionNode expression,
        ETokenType tagStartTokenType, Token endToken, Token blockStartToken)
    {
        var arguments = new List<ExpressionNode>(targets) { expression };
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

    private static ExpressionNode ParseAssignmentExpression(TokenIterator tokens, IExpressionParser expressionParser,
        SourceLocation tagStartLocation)
    {
        tokens.SkipWhitespace();
        ValidateEqualsToken(tokens, tagStartLocation);
        tokens.Consume(ETokenType.Equals);
        tokens.SkipWhitespace();

        // Custom parse: stop at BlockEnd or at identifier "loop" *only if* that "loop" is followed (ignoring spaces) by BlockEnd
        var exprTokens = new List<Token>();
        while (!tokens.IsAtEnd())
        {
            var t = tokens.Peek();
            if (t.Type == ETokenType.BlockEnd)
            {
                break;
            }

            if (t.Type == ETokenType.Identifier && t.Value.Equals("loop", StringComparison.OrdinalIgnoreCase))
            {
                // Look ahead to next non-space/text token to determine if 'loop' is the trailing marker
                int lookahead = 1;
                Token la;
                while (true)
                {
                    la = tokens.Peek(lookahead);
                    // Treat Text tokens that are only spaces/tabs as ignorable whitespace
                    if (la.Type == ETokenType.Text && TokenIterator.IsSpaceOrTabOnly(la.Value))
                    {
                        lookahead++;
                        continue;
                    }
                    break;
                }

                if (la.Type == ETokenType.BlockEnd)
                {
                    // 'loop' is the trailing loop-scope marker -> stop collecting expression tokens
                    break;
                }

                // Otherwise 'loop' is part of the expression (e.g. loop.index0) => consume as normal
            }

            exprTokens.Add(tokens.Consume(t.Type));
        }

        // Now parse the collected tokens as an expression
        var exprIterator = new TokenIterator(exprTokens);
        return expressionParser.Parse(exprIterator);
    }

    private List<ExpressionNode> ParseTargets(TokenIterator tokens, IExpressionParser expressionParser)
    {
        var targets = new List<ExpressionNode>();
        tokens.SkipWhitespace();

        // Consume the 'set' keyword
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Identifier)
        {
            tokens.Consume(ETokenType.Identifier);
        }

        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.Equals && tokens.Peek().Type != ETokenType.BlockEnd)
        {
            // Parse target expression (identifier or attribute access)
            var target = expressionParser.Parse(tokens, ETokenType.Comma);
            targets.Add(target);

            tokens.SkipWhitespace();
            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
                tokens.SkipWhitespace();
            }
            else
            {
                break;
            }
        }
        return targets;
    }

    private static void ValidateEqualsToken(TokenIterator tokens, SourceLocation tagStartLocation)
    {
        tokens.SkipAllWhitespace();
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