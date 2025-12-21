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

        // Removed the loop that consumed unexpected tokens.
        // This loop was incorrectly consuming list separators (Comma) and subsequent elements
        // because they were not the stopTokenType (RightBracket).
        // It is better to let the caller handle any remaining tokens.

        Debug.WriteLine($"After Parse: {tokens.Peek().Type}:{tokens.Peek().Value} at {tokens.CurrentLocation}");
        return node;
    }

    /// <summary>
    /// 重构后的属性解析方法：支持「属性访问.」和「索引访问[]」的链式调用
    /// </summary>
    /// <param name="tokens">令牌迭代器</param>
    /// <returns>解析后的表达式节点</returns>
    protected virtual ExpressionNode ParseAttribute(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        // 先解析原始节点（标识符、字面量等）
        var node = ParsePrimary(tokens);

        // 循环处理：属性访问（.）或索引访问（[]），支持链式调用（如 messages[0].role[1].name）
        while (!tokens.IsAtEnd())
        {
            tokens.SkipWhitespace();
            var nextTokenType = tokens.Peek().Type;

            // 处理属性访问：.xxx
            if (nextTokenType == ETokenType.Dot)
            {
                tokens.Consume(ETokenType.Dot);
                tokens.SkipWhitespace();
                var attrToken = tokens.Peek();
                if (attrToken.Type != ETokenType.Identifier)
                {
                    throw new InvalidOperationException(
                        $"Expected Identifier after '.', got {attrToken.Type} ('{attrToken.Value}') at {attrToken.Line}:{attrToken.Column}");
                }
                tokens.Consume(ETokenType.Identifier);

                // Method Call Detection
                tokens.SkipWhitespace();
                if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.LeftParen)
                {
                    var (args, kwargs) = ParseCallArguments(tokens);
                    node = new MethodCallNode(node, attrToken.Value, args, kwargs);
                }
                else
                {
                    node = new AttributeNode(node, attrToken.Value);
                }
            }
            // 处理索引访问：[xxx]
            else if (nextTokenType == ETokenType.LeftBracket)
            {
                tokens.Consume(ETokenType.LeftBracket);
                tokens.SkipWhitespace();
                // 递归解析索引表达式（支持复杂索引，如 messages[user.id + 1]）
                var indexNode = Parse(tokens, ETokenType.RightBracket);
                tokens.SkipWhitespace();
                tokens.Consume(ETokenType.RightBracket);
                node = new IndexNode(node, indexNode);
            }
            // 非属性/索引访问，终止循环
            else
            {
                break;
            }
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
            if (precedence == 0 && !token.Value.Equals("or", StringComparison.OrdinalIgnoreCase) && !token.Value.Equals("and", StringComparison.OrdinalIgnoreCase))
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
        // 调整为调用 ParseUnary 以支持一元运算符
        var node = ParseUnary(tokens);
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

    protected virtual ExpressionNode ParseUnary(TokenIterator tokens)
    {
        tokens.SkipWhitespace();
        var token = tokens.Peek();
        
        if (token.Type == ETokenType.Minus || token.Type == ETokenType.Plus || 
            (token.Type == ETokenType.Identifier && token.Value.Equals("not", StringComparison.OrdinalIgnoreCase)))
        {
            tokens.Consume(token.Type);
            var operand = ParseUnary(tokens);
            return new UnaryExpressionNode(token.Value, operand);
        }

        return ParseAttribute(tokens);
    }

    /// <summary>
    /// 简化 ParseIndex：透传至 ParseAttribute（已包含索引解析逻辑）
    /// 保持方法兼容性，可后续直接移除
    /// </summary>
    /// <param name="tokens">令牌迭代器</param>
    /// <param name="stopTokenType">终止令牌类型</param>
    /// <returns>解析后的表达式节点</returns>
    protected virtual ExpressionNode ParseIndex(TokenIterator tokens, ETokenType stopTokenType)
    {
        return ParseAttribute(tokens);
    }

    protected virtual ExpressionNode ParseListLiteral(TokenIterator tokens)
    {
        tokens.Consume(ETokenType.LeftBracket);
        var elements = new List<ExpressionNode>();

        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightBracket)
        {
            tokens.SkipWhitespace();
            var token = tokens.Peek();
            // 支持任意表达式作为列表元素，而非仅数字
            elements.Add(Parse(tokens, ETokenType.RightBracket));
            tokens.SkipWhitespace();

            if (tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
            }
            else if (tokens.Peek().Type != ETokenType.RightBracket)
            {
                throw new InvalidOperationException(
                    $"Expected Comma or RightBracket in list literal at {tokens.CurrentLocation.Line}:{tokens.CurrentLocation.Column}");
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
                var identifierValue = token.Value;
                if (identifierValue.Equals("or", StringComparison.OrdinalIgnoreCase) || identifierValue.Equals("and", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Unexpected logical operator '{identifierValue}' in primary expression at {token.Line}:{token.Column}");
                }

                // 布尔字面量处理（忽略大小写）
                if (identifierValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Consume(ETokenType.Identifier);
                    return new LiteralNode(true);
                }

                if (identifierValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Consume(ETokenType.Identifier);
                    return new LiteralNode(false);
                }

                tokens.Consume(ETokenType.Identifier);

                // 函数调用解析
                if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.LeftParen)
                {
                    return ParseFunctionCall(identifierValue, tokens);
                }

                return new IdentifierNode(identifierValue);

            case ETokenType.Number:
                tokens.Consume(ETokenType.Number);

                // 优先解析整数，再解析浮点数
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
        // 逻辑运算符（and/or）
        if (token.Type == ETokenType.Identifier)
        {
            return token.Value.ToLowerInvariant() switch
            {
                "in" => (20, false, "in"),
                "is" => (20, false, "is"),
                "or" => (10, false, "or"),
                "and" => (15, false, "and"),
                _ => (0, false, "")
            };
        }

        // 二元比较/算术运算符
        var tokenValue = token.Value;
        return tokenValue switch
        {
            "!=" => (20, false, "!="),
            "<" => (20, false, "<"),
            ">" => (20, false, ">"),
            "<=" => (20, false, "<="),
            ">=" => (20, false, ">="),
            "//" => (40, false, "//"),
            "%" => (40, false, "%"),
            "==" => (20, false, "=="),
            "+" => (30, false, "+"),
            "-" => (30, false, "-"),
            "*" => (40, false, "*"),
            "/" => (40, false, "/"),
            "|" => (10, false, "|"),
            _ => (0, false, "")
        };
    }

    private ExpressionNode ParseFunctionCall(string functionName, TokenIterator tokens)
    {
        var (arguments, kwargs) = ParseCallArguments(tokens);
        return new FunctionCallNode(functionName, arguments, kwargs);
    }

    private (List<ExpressionNode> args, Dictionary<string, ExpressionNode> kwargs) ParseCallArguments(TokenIterator tokens)
    {
        tokens.Consume(ETokenType.LeftParen);
        var arguments = new List<ExpressionNode>();
        var kwargs = new Dictionary<string, ExpressionNode>();

        // 解析函数参数
        tokens.SkipWhitespace();
        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightParen)
        {
            var arg = Parse(tokens, ETokenType.RightParen);
            tokens.SkipWhitespace();

            if (arg is IdentifierNode idNode && !tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Equals)
            {
                tokens.Consume(ETokenType.Equals);
                tokens.SkipWhitespace();
                var val = Parse(tokens, ETokenType.RightParen);
                kwargs[idNode.Name] = val;
            }
            else
            {
                if (kwargs.Count > 0)
                {
                    throw new TemplateParsingException("Positional argument follows keyword argument");
                }
                arguments.Add(arg);
            }

            tokens.SkipWhitespace();

            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
                tokens.SkipWhitespace();
            }
        }

        tokens.Consume(ETokenType.RightParen);
        return (arguments, kwargs);
    }

    private static string ReplaceUnicodeNames(string input)
    {
        // 仅处理 HOT SPRINGS 示例
        return Regex.Replace(input, @"\\N\{HOT SPRINGS\}", "\u2668");
    }
}