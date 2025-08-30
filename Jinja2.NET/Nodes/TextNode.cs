using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class TextNode : ASTNode, IVisitable
{
  public string Content { get; set; }
  public bool TrimLeft { get; set; }
  public bool TrimRight { get; set; }

  public TextNode(string content)
  {
    Content = content;
  }

  public override object Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new TextNodeRenderer();
  }
}