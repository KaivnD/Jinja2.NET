using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class BlockBodyParser : IBlockBodyParser
{
    private readonly IStatementParser _statementParser;

    public BlockBodyParser(IStatementParser statementParser)
    {
        _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
    }

    public List<ASTNode> Parse(TokenIterator tokens, params string[] stopKeywords)
    {
        var nodes = new List<ASTNode>();
        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.EOF && !IsBlockEnd(tokens, stopKeywords))
        {
            tokens.SkipWhitespace();
            // In BlockBodyParser.cs
            var (node, startMarkerType) = _statementParser.Parse(tokens, stopKeywords);
            if (node != null)
            {
                nodes.Add(node);
            }
        }

        // Skip whitespace before returning, so EndTagParser sees BlockStart
        tokens.SkipWhitespace();

        return nodes;
    }

    private bool IsBlockEnd(TokenIterator tokens, string[] endBlockNames)
    {
        tokens.SkipWhitespace();
        if (tokens.IsAtEnd() || tokens.Peek().Type == ETokenType.EOF)
        {
            return true;
        }

        var peek = tokens.Peek();
        if (peek.Type == ETokenType.BlockStart)
        {
            var next = tokens.Peek(1);
            if (next.Type == ETokenType.Identifier && endBlockNames.Contains(next.Value.ToLower()))
            {
                return true;
            }
        }

        return false;
    }
}