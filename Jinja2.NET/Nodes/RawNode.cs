using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class RawNode : ASTNode, IVisitable
{
  public string Content { get; }
  public ETokenType EndMarkerType { get; set; }
  public ETokenType StartMarkerType { get; set; }

  public RawNode(string content)
  {
    Content = content;
  }

  public override object? Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new RawNodeRenderer();
  }
}