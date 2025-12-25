using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class CallTagParser : BaseTagParser
{
    public override ASTNode? Parse(TokenIterator tokens, ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser, IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation,
        ETokenType tagStartTokenType)
    {
        var blockStartToken = GetBlockStartToken(tokens);
        var block = CreateBlockNode(TemplateConstants.BlockNames.Call, tagStartTokenType);

        SkipWhitespace(tokens);
        tokens.Consume(ETokenType.Identifier); // 'call'

        // Parse the callable expression up to the block end
        SkipWhitespace(tokens);
        var callable = expressionParser.Parse(tokens, ETokenType.BlockEnd);

        var endToken = ConsumeBlockEnd(tokens);

        // Add the callable expression as the single argument to the call block
        block.Arguments.Add(callable);
        ConfigureBlockNode(block, null, blockStartToken, endToken);

        // Parse body until endcall
        ParseBlockBody(tokens, blockBodyParser, block, TemplateConstants.BlockNames.EndCall);

        // Consume the endcall tag so outer parsers don't see the raw end tag
        tokens.SkipWhitespace();
        if (!tokens.IsAtEnd() && tokens.Peek().Type == ETokenType.BlockStart)
        {
            tokens.Consume(ETokenType.BlockStart);
            var endIdent = tokens.Consume(ETokenType.Identifier);
            if (!string.Equals(endIdent.Value, TemplateConstants.BlockNames.EndCall, StringComparison.OrdinalIgnoreCase))
            {
                throw CreateParseException($"Expected '{TemplateConstants.BlockNames.EndCall}' end tag, but found '{endIdent.Value}'", endIdent);
            }

            ConsumeBlockEnd(tokens);
        }

        return block;
    }

    private static Token GetBlockStartToken(TokenIterator tokens)
    {
        return tokens.Peek(-1);
    }
}
