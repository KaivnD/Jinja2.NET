using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface IExpressionParser
{
  ExpressionNode Parse(TokenIterator tokens, ETokenType stopTokenType = ETokenType.BlockEnd);
}