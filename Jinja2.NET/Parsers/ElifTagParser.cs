using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class ElifTagParser : BaseTagParser
{
    public override ASTNode Parse(
        TokenIterator tokens,
        ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser,
        IBlockBodyParser blockBodyParser,
        SourceLocation tagStartLocation,
        ETokenType tagStartTokenType)
    {
        var blockStartToken = tokens.Peek(-1);
        var block = CreateBlockNode(TemplateConstants.BlockNames.Elif, tagStartTokenType);
        SkipWhitespace(tokens);
        tokens.Consume(ETokenType.Identifier); // Consume "elif"

        ExpressionNode condition;
        try
        {
            condition = expressionParser.Parse(tokens);
        }
        catch (InvalidOperationException)
        {
            throw CreateParseException("Expected condition in elif block", tokens.CurrentLocation);
        }

        if (condition == null)
        {
            throw CreateParseException("Expected condition in elif block", tokens.CurrentLocation);
        }

        var endToken = ConsumeBlockEnd(tokens);
        ConfigureBlockNode(block, condition, blockStartToken, endToken);

        ParseBlockBody(tokens, blockBodyParser, block,
            TemplateConstants.BlockNames.Elif,
            TemplateConstants.BlockNames.Else,
            TemplateConstants.BlockNames.EndIf);

        // Validate and consume the end tag
        //SkipWhitespace(tokens);
        //if (tokens.IsAtEnd() || tokens.Peek().Type == ETokenType.EOF)
        //{
        //    throw CreateParseException("Expected 'endif' block at", tokens.CurrentLocation);
        //}

        //if (tokens.Peek().Type != ETokenType.BlockStart)
        //{
        //    throw CreateParseException("Expected 'endif' block", tokens.CurrentLocation);
        //}

        //tokens.Consume(ETokenType.BlockStart);
        //var endTag = tokens.Consume(ETokenType.Identifier);
        //if (!string.Equals(endTag.Value, TemplateConstants.BlockNames.EndIf, StringComparison.OrdinalIgnoreCase))
        //{
        //    throw CreateParseException($"Expected 'endif' block, but found '{endTag.Value}'", tokens.CurrentLocation);
        //}

        //tokens.Consume(ETokenType.BlockEnd);

        return block;
    }
}