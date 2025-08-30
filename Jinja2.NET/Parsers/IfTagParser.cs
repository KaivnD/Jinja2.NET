using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class IfTagParser : BaseTagParser
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
        var block = CreateBlockNode(TemplateConstants.BlockNames.If, tagStartTokenType);
        SkipWhitespace(tokens);
        tokens.Consume(ETokenType.Identifier); // "if"

        ExpressionNode condition;
        try
        {
            condition = expressionParser.Parse(tokens);
        }
        catch (InvalidOperationException)
        {
            throw CreateParseException("Expected condition in if block", tokens.CurrentLocation);
        }

        if (condition == null)
        {
            throw CreateParseException("Expected condition in if block", tokens.CurrentLocation);
        }

        var endToken = ConsumeBlockEnd(tokens);
        ConfigureBlockNode(block, condition, blockStartToken, endToken);

        // This will stop at elif, else, or endif and let the statement parser handle the next tag
        ParseBlockBody(tokens, blockBodyParser, block,
            TemplateConstants.BlockNames.Elif,
            TemplateConstants.BlockNames.Else,
            TemplateConstants.BlockNames.EndIf);

        return block;
    }
}