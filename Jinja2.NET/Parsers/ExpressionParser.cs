using System.Diagnostics;
using System.Text.RegularExpressions;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class ExpressionParser : IExpressionParser
{
    public virtual ExpressionNode Parse(TokenIterator tokens, ETokenType stopTokenType = ETokenType.BlockEnd)
    {
        tokens.SkipWhitespace();
        if (tokens.IsAtEnd())
        {
            throw new TemplateParsingException(
                $"Expected expression, but found end of input at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}");
        }

        var nextToken = tokens.Peek();
        if (nextToken.Type == stopTokenType ||
            nextToken.Type == ETokenType.BlockStart ||
            nextToken.Type == ETokenType.EOF)
        {
            throw new TemplateParsingException(
                $"Expected expression, but found {nextToken.Type} at {nextToken.Line}:{nextToken.Column}");
        }

        var node = ParseBinary(tokens, 0, stopTokenType);
        tokens.SkipWhitespace();

        // DON'T consume tokens that might be valid keywords for other parsers
        // Only consume tokens that are definitely syntax errors
        while (!tokens.IsAtEnd() && tokens.Peek().Type != stopTokenType)
        {
            var token = tokens.Peek();

            // Don't consume tokens that might be valid keywords
            if (token.Type == ETokenType.Identifier &&
                (token.Value.Equals("recursive", StringComparison.OrdinalIgnoreCase) ||
                 token.Value.Equals("else", StringComparison.OrdinalIgnoreCase) ||
                 token.Value.Equals("elif", StringComparison.OrdinalIgnoreCase) ||
                 token.Value.Equals("endif", StringComparison.OrdinalIgnoreCase) ||
                 token.Value.Equals("endfor", StringComparison.OrdinalIgnoreCase)))
            {
                // Stop here - let the calling parser handle these keywords
                break;
            }

            Debug.WriteLine(
                $"Unexpected token in expression: {token.Type}:{token.Value} at {tokens.CurrentLocation}");
            tokens.Consume(token.Type);
        }

        Debug.WriteLine($"After Parse: {tokens.Peek().Type}:{tokens.Peek().Value} at {tokens.CurrentLocation}");
        return node;
    }
    protected virtual ExpressionNode ParseAttribute(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        var node = ParsePrimary(tokens);
        while (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Dot)
        {
            tokens.Consume(ETokenType.Dot);
            tokens.SkipWhitespace();
            var nextToken = tokens.Peek();
            if (nextToken.Type != ETokenType.Identifier)
            {
                throw new InvalidOperationException(
                    $"Expected Identifier after '.', got {nextToken.Type} ('{nextToken.Value}') at {nextToken.Line}:{nextToken.Column}");
            }

            var attrToken = tokens.Consume(ETokenType.Identifier);
            node = new AttributeNode(node, attrToken.Value);
        }

        return node;
    }

    protected virtual ExpressionNode ParseBinary(TokenIterator tokens, int parentPrecedence, ETokenType stopTokenType)
    {
        tokens.SkipWhitespace();
        var left = ParseFilter(tokens, stopTokenType);
        while (!tokens.IsAtEnd() && tokens.Peek().Type != stopTokenType)
        {
            tokens.SkipWhitespace();
            var token = tokens.Peek();
            var (precedence, isRightAssociative, op) = GetOperatorPrecedence(token);
            if (precedence == 0 && !token.Value.Equals("or") && !token.Value.Equals("and"))
            {
                break;
            }

            if (precedence < parentPrecedence || (precedence == parentPrecedence && !isRightAssociative))
            {
                break;
            }

            tokens.Consume(token.Type);
            tokens.SkipWhitespace();
            var right = ParseBinary(tokens, isRightAssociative ? precedence - 1 : precedence, stopTokenType);
            left = new BinaryExpressionNode(left, op, right);
        }

        return left;
    }

    protected virtual ExpressionNode ParseFilter(TokenIterator tokens, ETokenType stopTokenType)
    {
        tokens.SkipWhitespace();
        var node = ParseIndex(tokens, stopTokenType);
        while (!tokens.IsAtEnd() && tokens.Peek().Type != stopTokenType)
        {
            tokens.SkipWhitespace();
            if (tokens.Peek().Type != ETokenType.Pipe)
            {
                break;
            }

            tokens.Consume(ETokenType.Pipe);
            tokens.SkipWhitespace();
            if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.Identifier)
            {
                throw new InvalidOperationException(
                    $"Expected Identifier after '|' at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}");
            }

            var filterToken = tokens.Consume(ETokenType.Identifier);
            var args = new List<ExpressionNode>();
            tokens.SkipWhitespace();
            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.LeftParen)
            {
                tokens.Consume(ETokenType.LeftParen);
                tokens.SkipWhitespace();
                while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightParen)
                {
                    args.Add(Parse(tokens, ETokenType.RightParen));
                    tokens.SkipWhitespace();
                    if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
                    {
                        tokens.Consume(ETokenType.Comma);
                    }

                    tokens.SkipWhitespace();
                }

                tokens.Consume(ETokenType.RightParen);
                tokens.SkipWhitespace();
            }

            node = new FilterNode(node, filterToken.Value, args);
        }

        return node;
    }

    protected virtual ExpressionNode ParseIndex(TokenIterator tokens, ETokenType stopTokenType)
    {
        tokens.SkipWhitespace();
        var node = ParseAttribute(tokens);
        while (!tokens.IsAtEnd() && tokens.Peek().Type != stopTokenType)
        {
            tokens.SkipWhitespace();
            if (tokens.Peek().Type != ETokenType.LeftBracket)
            {
                break;
            }

            tokens.Consume(ETokenType.LeftBracket);
            tokens.SkipWhitespace();
            var index = Parse(tokens, ETokenType.RightBracket);
            tokens.SkipWhitespace();
            tokens.Consume(ETokenType.RightBracket);
            node = new IndexNode(node, index);
        }

        return node;
    }

    protected virtual ExpressionNode ParseListLiteral(TokenIterator tokens)
    {
        tokens.Consume(ETokenType.LeftBracket);
        var elements = new List<ExpressionNode>();

        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightBracket)
        {
            var token = tokens.Peek();
            if (token.Type == ETokenType.Number)
            {
                tokens.Consume(ETokenType.Number);
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
                break; // Stop for non-number tokens
            }

            if (tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
            }
            else if (tokens.Peek().Type != ETokenType.RightBracket)
            {
                break; // Prevent over-consumption
            }
        }

        tokens.Consume(ETokenType.RightBracket);
        return new ListLiteralNode(elements);
    }

    protected virtual ExpressionNode ParsePrimary(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        if (tokens.IsAtEnd())
        {
            throw new InvalidOperationException(
                $"Expected expression, but found end of input at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}");
        }

        var token = tokens.Peek();
        switch (token.Type)
        {
            case ETokenType.Identifier:
                if (token.Value == "or" || token.Value == "and")
                {
                    throw new InvalidOperationException(
                        $"Unexpected logical operator '{token.Value}' in primary expression at {token.Line}:{token.Column}");
                }

                // Add boolean literal handling
                if (token.Value == "true")
                {
                    tokens.Consume(ETokenType.Identifier);
                    return new LiteralNode(true);
                }

                if (token.Value == "false")
                {
                    tokens.Consume(ETokenType.Identifier);
                    return new LiteralNode(false);
                }

                tokens.Consume(ETokenType.Identifier);

                // Check for function call syntax
                if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.LeftParen)
                {
                    return ParseFunctionCall(token.Value, tokens);
                }

                return new IdentifierNode(token.Value);

            case ETokenType.Number:
                tokens.Consume(ETokenType.Number);

                // Try to parse as int first, then double
                if (int.TryParse(token.Value, out var intNumber))
                {
                    return new LiteralNode(intNumber);
                }

                if (double.TryParse(token.Value, out var doubleNumber))
                {
                    return new LiteralNode(doubleNumber);
                }

                throw new InvalidOperationException($"Invalid number: '{token.Value}' at {token.Line}:{token.Column}");
            case ETokenType.String:
                tokens.Consume(ETokenType.String);
                if (token.Value.Length < 2)
                {
                    throw new InvalidOperationException(
                        $"String token too short: '{token.Value}' at {token.Line}:{token.Column}");
                }

                var raw = token.Value.Substring(1, token.Value.Length - 2);
                raw = ReplaceUnicodeNames(raw);
                var strValue = Regex.Unescape(raw);
                return new LiteralNode(strValue);

            case ETokenType.LeftParen:
                tokens.Consume(ETokenType.LeftParen);
                tokens.SkipWhitespace();
                var expr = Parse(tokens, ETokenType.RightParen);
                tokens.SkipWhitespace();
                tokens.Consume(ETokenType.RightParen);
                return expr;

            case ETokenType.LeftBracket:
                return ParseListLiteral(tokens);

            default:
                throw new InvalidOperationException(
                    $"Expected Identifier, Number, String, or '(', got {token.Type} ('{token.Value}') at {token.Line}:{token.Column}");
        }
    }

    private (int Precedence, bool IsRightAssociative, string Operator) GetOperatorPrecedence(Token token)
    {
        if (token.Type == ETokenType.Identifier)
        {
            return token.Value switch
            {
                "in" => (20, false, "in"),
                "is" => (20, false, "is"),
                "or" => (10, false, "or"),
                "and" => (15, false, "and"),
                _ => (0, false, "")
            };
        }

        if (token.Type == ETokenType.Text || token.Type == ETokenType.Operator)
        {
            return token.Value switch
            {
                "!=" => (20, false, "!="),
                "<" => (20, false, "<"),
                ">" => (20, false, ">"),
                "<=" => (20, false, "<="),
                ">=" => (20, false, ">="),
                "//" => (40, false, "//"),
                "%" => (40, false, "%"),
                "==" => (20, false, "=="),
                _ => (0, false, "")
            };
        }

        return token.Type switch
        {
            ETokenType.Pipe => (10, false, "|"),
            ETokenType.Equals => (20, false, "=="),
            ETokenType.Plus => (30, false, "+"),
            ETokenType.Minus => (30, false, "-"),
            ETokenType.Multiply => (40, false, "*"),
            ETokenType.Divide => (40, false, "/"),
            _ => (0, false, "")
        };
    }

    private ExpressionNode ParseFunctionCall(string functionName, TokenIterator tokens)
    {
        tokens.Consume(ETokenType.LeftParen);
        var arguments = new List<ExpressionNode>();

        // Parse function arguments
        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightParen)
        {
            arguments.Add(Parse(tokens, ETokenType.RightParen));

            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
            }
        }

        tokens.Consume(ETokenType.RightParen);
        return new FunctionCallNode(functionName, arguments);
    }

    private static string ReplaceUnicodeNames(string input)
    {
        // Only handle HOT SPRINGS for now
        return Regex.Replace(input, @"\\N\{HOT SPRINGS\}", "\u2668");
    }
}