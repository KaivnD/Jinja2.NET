using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class IndexNode : ExpressionNode, IVisitable
{
    public ExpressionNode Index { get; }
    public ExpressionNode Target { get; }

    public IndexNode(ExpressionNode target, ExpressionNode index)
    {
        Target = target;
        Index = index;
    }

    public override object Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new IndexNodeRenderer();
    }
}