using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class LiteralNode : ExpressionNode, IVisitable
{
    public object Value { get; set; }

    public LiteralNode(object value)
    {
        Value = value;
    }

    public override object Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new LiteralNodeRenderer();
    }
}