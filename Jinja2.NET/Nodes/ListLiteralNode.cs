using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class ListLiteralNode : ExpressionNode, IVisitable
{
    public List<ExpressionNode> Elements { get; }

    public ListLiteralNode(List<ExpressionNode> elements)
    {
        Elements = elements;
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new ListLiteralNodeRenderer();
    }
}