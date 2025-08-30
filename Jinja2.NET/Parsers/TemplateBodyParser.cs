using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class TemplateBodyParser
{
    private readonly IStatementParser _statementParser;

    public TemplateBodyParser(IStatementParser statementParser)
    {
        _statementParser = statementParser ?? throw new ArgumentNullException(nameof(statementParser));
    }

    public TemplateNode Parse(TokenIterator tokens)
    {
        var template = new TemplateNode();
        TextNode lastTextNode = null;
        var trimNextTextNodeLeading = false;

        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.EOF)
        {
            //tokens.SkipWhitespace();
            var (node, startMarkerType) = _statementParser.Parse(tokens);

            // --- Apply trailing whitespace trim to the previous TextNode if needed ---
            if (lastTextNode != null &&
                (startMarkerType == ETokenType.VariableStart || startMarkerType == ETokenType.BlockStart ||
                 startMarkerType == ETokenType.CommentStart) &&
                tokens.Peek(-1).TrimLeft)
            {
                lastTextNode.TrimRight = true;
            }

            if (node is TextNode textNode)
            {
                // --- Apply leading whitespace trim if previous block/variable/comment end had TrimRight ---
                textNode.TrimLeft = trimNextTextNodeLeading;
                trimNextTextNodeLeading = false;

                lastTextNode = textNode;
            }
            else
            {
                lastTextNode = null;
            }

            // --- Set flag to trim leading whitespace of the next TextNode if this node is a block/variable/comment end with TrimRight ---
            if ((startMarkerType == ETokenType.VariableEnd || startMarkerType == ETokenType.BlockEnd ||
                 startMarkerType == ETokenType.CommentEnd) &&
                tokens.Peek(-1).TrimRight)
            {
                trimNextTextNodeLeading = true;
            }

            if (node != null)
            {
                template.Children.Add(node);
            }
        }

        return template;
    }
}