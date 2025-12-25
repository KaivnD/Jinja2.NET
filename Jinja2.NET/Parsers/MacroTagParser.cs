using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class MacroTagParser : BaseTagParser
{
    public override ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser, IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation,
        ETokenType tagStartTokenType)
    {
        var blockStartToken = GetBlockStartToken(tokens);
        var block = CreateBlockNode(TemplateConstants.BlockNames.Macro, tagStartTokenType);

        SkipWhitespace(tokens);
        tokens.Consume(ETokenType.Identifier); // "macro"

        // Expect macro name
        SkipWhitespace(tokens);
        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.Identifier)
        {
            throw CreateParseException("Expected macro name", tagStartLocation);
        }

        var nameToken = tokens.Consume(ETokenType.Identifier);
        var macroName = nameToken.Value;

        // Require parameter list (even if empty)
        SkipWhitespace(tokens);
        var parameters = new List<ExpressionNode>();
        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.LeftParen)
        {
            throw CreateParseException("Expected '(' after macro name", tokens.CurrentLocation);
        }

        tokens.Consume(ETokenType.LeftParen);
        SkipWhitespace(tokens);
        while (!tokens.IsAtEnd() && tokens.Peek().Type != ETokenType.RightParen)
        {
            if (tokens.Peek().Type == ETokenType.Identifier)
            {
                var id = tokens.Consume(ETokenType.Identifier);
                parameters.Add(new IdentifierNode(id.Value));

                // 支持参数默认值（例如: name=True 或 name="default"）
                SkipWhitespace(tokens);
                if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Equals)
                {
                    // 消耗 '=' 并解析后续的表达式，直到逗号或右括号，但不将其作为参数加入列表
                    tokens.Consume(ETokenType.Equals);
                    SkipWhitespace(tokens);
                    // 使用传入的表达式解析器来解析默认值
                    expressionParser.Parse(tokens, ETokenType.RightParen);
                    SkipWhitespace(tokens);
                }
            }

            SkipWhitespace(tokens);
            if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.Comma)
            {
                tokens.Consume(ETokenType.Comma);
                SkipWhitespace(tokens);
            }
            else
            {
                break;
            }
        }

        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.RightParen)
        {
            throw CreateParseException("Expected ')' to close macro parameter list", tokens.CurrentLocation);
        }

        tokens.Consume(ETokenType.RightParen);

        var endToken = ConsumeBlockEnd(tokens);

        // Create block node: arguments: first argument is macro name as LiteralNode, followed by IdentifierNodes for params
        var args = new List<ExpressionNode> { new LiteralNode(macroName) };
        args.AddRange(parameters);

        block.Arguments.AddRange(args);
        ConfigureBlockNode(block, null, blockStartToken, endToken);

        // Parse body until endmacro
        ParseBlockBody(tokens, blockBodyParser, block, TemplateConstants.BlockNames.EndMacro);

        return block;
    }

    private static Token GetBlockStartToken(TokenIterator tokens)
    {
        return tokens.Peek(-1);
    }
}
