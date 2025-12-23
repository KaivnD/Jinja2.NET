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
            // Do not aggressively skip text-only whitespace here — keep significant spaces in block bodies
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
        // Look ahead for a BlockStart while preserving whitespace Text tokens
        if (tokens.IsAtEnd() || tokens.Peek().Type == ETokenType.EOF)
        {
            return true;
        }

        var lookahead = 0;
        while (!tokens.IsAtEnd() && tokens.Peek(lookahead).Type == ETokenType.Text && TokenIterator.IsSpaceOrTabOnly(tokens.Peek(lookahead).Value))
        {
            lookahead++;
        }

        var candidate = tokens.Peek(lookahead);
        if (candidate.Type == ETokenType.BlockStart)
        {
            var next = tokens.Peek(lookahead + 1);
            if (next.Type == ETokenType.Identifier)
            {
                var lower = next.Value.ToLower();
                // If the identifier matches one of the stop keywords, this is a block end
                if (endBlockNames.Contains(lower))
                {
                    return true;
                }

                // Also treat any identifier that looks like an end-tag (starts with 'end') as a potential end
                // so the enclosing parser can validate and raise a helpful error for invalid end tags.
                if (lower.StartsWith("end"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}