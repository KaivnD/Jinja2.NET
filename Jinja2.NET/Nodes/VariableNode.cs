using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class VariableNode : ASTNode, IVisitable
{
  public ETokenType EndMarkerType { get; set; }
  public ExpressionNode Expression { get; set; }
  public ETokenType StartMarkerType { get; set; }
  public bool TrimLeft { get; set; }
  public bool TrimRight { get; set; }

  public VariableNode(ExpressionNode expression)
  {
    Expression = expression;
  }

  public override object? Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new VariableNodeRenderer();
  }
}