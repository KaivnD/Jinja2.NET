using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class IdentifierNode : ExpressionNode, IVisitable
{
    public string Name { get; set; }

    public IdentifierNode(string name)
    {
        Name = name;
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new IdentifierNodeRenderer();
    }
}