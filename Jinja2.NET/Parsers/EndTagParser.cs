using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class EndTagParser : BaseTagParser
{
    private readonly string _tagName;

    public EndTagParser(string tagName)
    {
        _tagName = tagName?.ToLower() ?? throw new ArgumentNullException(nameof(tagName));
    }

    public override ASTNode? Parse(TokenIterator tokens, ITagParserRegistry? tagRegistry,
        IExpressionParser? expressionParser, IBlockBodyParser? blockBodyParser,
        SourceLocation tagStartLocation, ETokenType tagStartTokenType)
    {
        SkipWhitespace(tokens);
        tokens.Consume(ETokenType.Identifier); // Consume end tag name
        ConsumeBlockEnd(tokens);
        return null; // End tags produce no AST node
    }
}