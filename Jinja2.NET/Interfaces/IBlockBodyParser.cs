using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface IBlockBodyParser
{
  List<ASTNode> Parse(TokenIterator tokens, params string[] stopKeywords);
}