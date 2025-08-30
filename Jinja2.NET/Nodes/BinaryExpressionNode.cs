using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class BinaryExpressionNode : ExpressionNode, IVisitable
{
    public ExpressionNode Left { get; }
    public string Operator { get; }
    public ExpressionNode Right { get; }

    public BinaryExpressionNode(ExpressionNode left, string op, ExpressionNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new BinaryExpressionNodeRenderer();
    }
}