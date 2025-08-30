using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface IStatementParser
{
  (ASTNode Node, ETokenType ConsumedStartMarkerType) Parse(TokenIterator tokens);
  (ASTNode Node, ETokenType ConsumedStartMarkerType) Parse(TokenIterator tokens, params string[] stopKeywords);
}