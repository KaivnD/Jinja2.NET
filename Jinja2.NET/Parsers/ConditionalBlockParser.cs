using Jinja2.NET.Interfaces;
using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Parsers;

public class ConditionalBlockParser : BaseTagParser
{
    public override ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry,
        IExpressionParser expressionParser, IBlockBodyParser blockBodyParser,
        SourceLocation tagStartLocation, ETokenType tagStartTokenType)
    {
        // Parse the initial if block directly (do NOT use registry to avoid recursion)
        var ifParser = new IfTagParser();
        var rootBlock = ifParser.Parse(tokens, tagRegistry, expressionParser, blockBodyParser, tagStartLocation,
            tagStartTokenType) as BlockNode;
        if (rootBlock == null)
        {
            throw CreateParseException("Expected BlockNode for if block", tagStartLocation);
        }

        // Parse all elif/else branches using the registry
        while (true)
        {
            tokens.SkipWhitespace();
            if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.BlockStart)
            {
                break;
            }

            var lookahead = tokens.Peek(1);
            if (lookahead.Type != ETokenType.Identifier)
            {
                break;
            }

            var tag = lookahead.Value.ToLowerInvariant();
            if (tag == TemplateConstants.BlockNames.Elif || tag == TemplateConstants.BlockNames.Else)
            {
                var branchParser = tagRegistry.GetParser(tag);
                if (branchParser == null)
                {
                    throw CreateParseException($"No parser registered for '{tag}'", tokens.CurrentLocation);
                }

                // Consume the BlockStart so the branch parser sees the Identifier next
                tokens.Consume(ETokenType.BlockStart);

                var branchBlock = branchParser.Parse(tokens, tagRegistry, expressionParser, blockBodyParser,
                    tokens.CurrentLocation, ETokenType.BlockStart);
                if (branchBlock != null)
                {
                    rootBlock.Children.Add(branchBlock);
                }

                continue;
            }

            break;
        }

        // Expect endif
        tokens.SkipWhitespace();
        if (tokens.IsAtEnd() || tokens.Peek().Type != ETokenType.BlockStart)
        {
            throw CreateParseException("Unclosed 'if' block: expected 'endif' block", tokens.CurrentLocation);
        }

        tokens.Consume(ETokenType.BlockStart);
        var endTag = tokens.Consume(ETokenType.Identifier);
        if (!string.Equals(endTag.Value, TemplateConstants.BlockNames.EndIf, StringComparison.OrdinalIgnoreCase))
        {
            throw CreateParseException($"Expected 'endif' block, but found '{endTag.Value}'", tokens.CurrentLocation);
        }

        tokens.Consume(ETokenType.BlockEnd);

        return rootBlock;
    }
}