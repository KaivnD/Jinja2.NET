using Jinja2.NET.Models;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface ITagParser
{
  ASTNode Parse(TokenIterator tokens, ITagParserRegistry tagRegistry, IExpressionParser expressionParser, IBlockBodyParser blockBodyParser, SourceLocation tagStartLocation, ETokenType tagStartTokenType);
}