using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes;

public class ConditionalExpressionNode : ExpressionNode, IVisitable
{
    public ExpressionNode Condition { get; }
    public ExpressionNode TrueExpression { get; }
    public ExpressionNode FalseExpression { get; }

    public ConditionalExpressionNode(ExpressionNode condition, ExpressionNode trueExpr, ExpressionNode falseExpr)
    {
        Condition = condition;
        TrueExpression = trueExpr;
        FalseExpression = falseExpr;
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new Nodes.Renderers.ConditionalExpressionNodeRenderer();
    }
}
