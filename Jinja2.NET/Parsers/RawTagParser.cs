using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class RawTagParser : ITagParser
{
    public ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser, IBlockBodyParser blockBodyParser,
        SourceLocation tagStartLocation, ETokenType tagStartTokenType)
    {
        // Consume {% raw %}
        tokens.Consume(ETokenType.Identifier); // "raw"
        tokens.Consume(ETokenType.BlockEnd);

        // Collect all text until {% endraw %}
        var content = new StringBuilder();
        while (!tokens.IsAtEnd())
        {
            if (tokens.Peek().Type == ETokenType.BlockStart &&
                tokens.Peek(1).Type == ETokenType.Identifier &&
                tokens.Peek(1).Value == "endraw")
            {
                break;
            }

            content.Append(tokens.Consume(tokens.Peek().Type).Value);
        }

        // Consume {% endraw %}
        tokens.Consume(ETokenType.BlockStart);
        tokens.Consume(ETokenType.Identifier); // "endraw"
        tokens.Consume(ETokenType.BlockEnd);

        return new RawNode(content.ToString());
    }
}