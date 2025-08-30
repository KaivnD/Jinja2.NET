using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class FilterNode : ExpressionNode, IVisitable
{
  public List<ExpressionNode> Arguments { get; } = new();
  public ExpressionNode Expression { get; }
  public string FilterName { get; }

  public FilterNode(ExpressionNode expression, string filterName)
  {
    Expression = expression;
    FilterName = filterName;
  }

  public FilterNode(ExpressionNode expression, string filterName, List<ExpressionNode> arguments)
  {
    Expression = expression;
    FilterName = filterName;
    Arguments.AddRange(arguments);
  }

  public override object Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new FilterNodeRenderer();
  }
}