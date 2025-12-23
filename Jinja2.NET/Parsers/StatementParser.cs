using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class StatementParser : IStatementParser
{
    private readonly LexerConfig _config;
    private readonly IExpressionParser _expressionParser;
    private readonly ITagParserRegistry _tagRegistry;

    public StatementParser(IExpressionParser? expressionParser, ITagParserRegistry? tagRegistry,
        LexerConfig? config = null)
    {
        _expressionParser = expressionParser ?? throw new ArgumentNullException(nameof(expressionParser));
        _tagRegistry = tagRegistry ?? throw new ArgumentNullException(nameof(tagRegistry));
        _config = config ?? new LexerConfig();
    }

    public (ASTNode Node, ETokenType ConsumedStartMarkerType) Parse(TokenIterator tokens)
    {
        return Parse(tokens, []);
    }


    public (ASTNode Node, ETokenType ConsumedStartMarkerType) Parse(TokenIterator tokens, params string[] stopKeywords)
    {
        //tokens.SkipWhitespace();
        if (tokens.IsAtEnd() || tokens.Peek().Type == ETokenType.EOF)
        {
            return (null, ETokenType.EOF);
        }

        var token = tokens.Peek();

        // Check for stop keywords before parsing a block
        if (token.Type == ETokenType.BlockStart && stopKeywords != null && stopKeywords.Length > 0)
        {
            var lookahead = tokens.Peek(1);
            if (lookahead.Type == ETokenType.Identifier && stopKeywords.Contains(lookahead.Value.ToLower()))
            {
                // Stop before consuming the end tag
                return (null, ETokenType.BlockStart);
            }
        }

        return token.Type switch
        {
            ETokenType.BlockStart => ParseBlock(tokens),
            ETokenType.Text => ParseText(tokens),
            ETokenType.VariableStart => ParseVariable(tokens),
            ETokenType.CommentStart => ParseComment(tokens),
            _ => throw new InvalidOperationException(
                $"Unexpected token: {token.Type} ('{token.Value}') at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}")
        };
    }

    private (ASTNode, ETokenType) ParseBlock(TokenIterator tokens)
    {
        var startToken = tokens.Consume(ETokenType.BlockStart);
        var tagStartLocation = tokens.CurrentLocation;
        var trimLeft = startToken.TrimLeft;

        tokens.SkipWhitespace();
        var blockNameToken = tokens.Peek();
        if (blockNameToken.Type != ETokenType.Identifier)
        {
            throw new InvalidOperationException(
                $"Expected Identifier at {blockNameToken.Line}:{blockNameToken.Column}");
        }

        var blockName = blockNameToken.Value.ToLower();
        var parser = _tagRegistry.GetParser(blockName);

        ASTNode? node;
        if (parser != null)
        {
            node = parser.Parse(tokens, _tagRegistry, _expressionParser, new BlockBodyParser(this), tagStartLocation,
                startToken.Type);
        }
        else
        {
            // Gracefully handle unknown block.
            // Support shorthand assignment syntax: `identifier = expression` as an implicit `set` block.
            var identifierToken = tokens.Consume(ETokenType.Identifier);
            tokens.SkipWhitespace();

            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Equals)
            {
                // Parse RHS expression until BlockEnd
                tokens.Consume(ETokenType.Equals);
                tokens.SkipWhitespace();
                var exprTokens = new List<Token>();
                while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.BlockEnd)
                {
                    exprTokens.Add(tokens.Consume(tokens.Peek().Type));
                }

                var endToken = tokens.Consume(ETokenType.BlockEnd);

                // Parse expression tokens
                var exprIterator = new TokenIterator(exprTokens);
                var expression = _expressionParser.Parse(exprIterator);

                // Create a set block: arguments = [ targetIdentifier, expression ]
                var target = new IdentifierNode(identifierToken.Value);
                var arguments = new List<ExpressionNode> { target, expression };
                var block = new BlockNode(TemplateConstants.BlockNames.Set, arguments, new List<ASTNode>())
                {
                    StartMarkerType = startToken.Type,
                    EndMarkerType = endToken.Type,
                    TrimLeft = trimLeft,
                    TrimRight = endToken.TrimRight
                };

                node = block;
            }
            else
            {
                // Unknown block: consume block end and return a generic BlockNode
                tokens.SkipWhitespace();
                tokens.Consume(ETokenType.BlockEnd);
                node = new BlockNode(blockName)
                {
                    StartMarkerType = startToken.Type,
                    EndMarkerType = ETokenType.BlockEnd,
                    TrimLeft = trimLeft
                };
            }
        }

        if (node is BlockNode blockNode)
        {
            blockNode.StartMarkerType = startToken.Type;
            blockNode.EndMarkerType = ETokenType.BlockEnd;
            blockNode.TrimLeft = trimLeft;
        }

        return (node, startToken.Type);
    }

    private (ASTNode, ETokenType) ParseComment(TokenIterator tokens)
    {
        var startToken = tokens.Consume(ETokenType.CommentStart);
        var contentBuilder = new StringBuilder();

        // Collect comment content until CommentEnd
        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.CommentEnd)
            contentBuilder.Append(tokens.Consume(tokens.Peek().Type).Value);

        var endToken = tokens.Consume(ETokenType.CommentEnd);

        // Create the CommentNode with trim flags
        var node = new CommentNode(
            contentBuilder.ToString(),
            startToken.TrimLeft,
            endToken.TrimRight
        );
        return (node, startToken.Type);
    }

    private (ASTNode, ETokenType) ParseText(TokenIterator tokens)
    {
        var token = tokens.Consume(ETokenType.Text);
        var trimTrailing = _config.LstripBlocks && token.Value.StartsWith("\r\n") &&
                           token.Value.Substring(2).All(c => c == ' ' || c == '\t');
        return (new TextNode(token.Value)
        {
            TrimLeft = token.TrimLeft,
            TrimRight = token.TrimRight || trimTrailing
        }, ETokenType.Text);
        //var token = tokens.Consume(ETokenType.Text);
        //return (
        //    new TextNode(token.Value)
        //        { TrimLeadingWhitespace = token.TrimLeft, TrimTrailingWhitespace = token.TrimRight }, ETokenType.Text);
    }

    private (ASTNode, ETokenType) ParseVariable(TokenIterator tokens)
    {
        var startToken = tokens.Consume(ETokenType.VariableStart);
        tokens.SkipWhitespace();
        var expression = _expressionParser.Parse(tokens, ETokenType.VariableEnd);
        tokens.SkipWhitespace();
        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.VariableEnd)
        {
            throw new InvalidOperationException(
                $"Expected VariableEnd at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}");
        }

        var endToken = tokens.Consume(ETokenType.VariableEnd);
        var node = new VariableNode(expression)
        {
            StartMarkerType = startToken.Type,
            EndMarkerType = endToken.Type,
            TrimLeft = startToken.TrimLeft,
            TrimRight = endToken.TrimRight
        };
        return (node, startToken.Type);
    }
}