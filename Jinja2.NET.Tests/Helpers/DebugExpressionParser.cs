using Jinja2.NET.Nodes;
using Jinja2.NET.Parsers;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests;

public class DebugExpressionParser : ExpressionParser
{
    private readonly ITestOutputHelper _output;

    public DebugExpressionParser(ITestOutputHelper output)
    {
        _output = output;
    }

    protected override ExpressionNode ParseListLiteral(TokenIterator tokens)
    {
        tokens.Consume(ETokenType.LeftBracket);
        var elements = new List<ExpressionNode>();
        _output.WriteLine($"Debug: Start ParseListLiteral, token: {tokens.Peek().Type} [{tokens.Peek().Value}]");

        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightBracket)
        {
            var token = tokens.Peek();
            if (token.Type == ETokenType.Number)
            {
                tokens.Consume(ETokenType.Number);
                _output.WriteLine(
                    $"Debug: Parsed number {token.Value}, next token: {tokens.Peek().Type} [{tokens.Peek().Value}]");
                if (double.TryParse(token.Value, out var number))
                {
                    elements.Add(new LiteralNode(number));
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Invalid number: '{token.Value}' at {token.Line}:{token.Column}");
                }
            }
            else
            {
                _output.WriteLine($"Debug: Unexpected token {token.Type} [{token.Value}], breaking");
                break;
            }

            if (tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
                _output.WriteLine($"Debug: Consumed comma, next token: {tokens.Peek().Type} [{tokens.Peek().Value}]");
            }
            else if (tokens.Peek().Type != ETokenType.RightBracket)
            {
                _output.WriteLine($"Debug: Unexpected token {tokens.Peek().Type} [{tokens.Peek().Value}], breaking");
                break;
            }
        }

        tokens.Consume(ETokenType.RightBracket);
        _output.WriteLine($"Debug: End ParseListLiteral, elements: {elements.Count}");
        return new ListLiteralNode(elements);
    }
}