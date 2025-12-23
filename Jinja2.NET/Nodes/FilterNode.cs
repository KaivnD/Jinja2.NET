using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class FilterNode : ExpressionNode, IVisitable
{
  public List<ExpressionNode> Arguments { get; } = new();
  public Dictionary<string, ExpressionNode> Kwargs { get; } = new();
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

  public FilterNode(ExpressionNode expression, string filterName, List<ExpressionNode> arguments, Dictionary<string, ExpressionNode> kwargs)
  {
    Expression = expression;
    FilterName = filterName;
    Arguments.AddRange(arguments);
    foreach (var kv in kwargs) Kwargs[kv.Key] = kv.Value;
  }

  public override object? Accept(INodeVisitor visitor)
  {
    return visitor.Visit(this);
  }

  public INodeRenderer GetRenderer()
  {
    return new FilterNodeRenderer();
  }
}