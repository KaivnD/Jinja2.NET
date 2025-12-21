using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;
using Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

namespace Jinja2.NET.Nodes;

public class BlockNode : ASTNode, IVisitable
{
  public List<ExpressionNode> Arguments { get; } = new();
  public List<ASTNode> Children { get; } = new();
  public ETokenType EndMarkerType { get; set; }
  public string Name { get; }
  public ETokenType StartMarkerType { get; set; }
  public bool TrimLeft { get; set; }
  public bool TrimRight { get; set; }
  public bool TrimBodyLeft { get; set; }
  public bool TrimBodyRight { get; set; }
  public bool IsLoopScoped { get; set; } 

    public BlockNode(string name)
  {
    Name = name.ToLower();
  }

  public BlockNode(string name, List<ExpressionNode> arguments, List<ASTNode> children)
  {
    Name = name.ToLower();
    Arguments.AddRange(arguments);
    Children.AddRange(children);
  }

  public override object? Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new BlockNodeRenderer();
  }
}