using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class SliceNode : ExpressionNode, IVisitable
{
    public ExpressionNode? Start { get; }
    public ExpressionNode? Stop { get; }
    public ExpressionNode? Step { get; }

    public SliceNode(ExpressionNode? start, ExpressionNode? stop, ExpressionNode? step)
    {
        Start = start;
        Stop = stop;
        Step = step;
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new IndexNodeRenderer();
    }
}
