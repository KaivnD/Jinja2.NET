using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class AttributeNode : ExpressionNode, IVisitable
{
    public string Attribute { get; set; }
    public ExpressionNode Object { get; set; }

    public AttributeNode(ExpressionNode obj, string attribute)
    {
        Object = obj;
        Attribute = attribute;
    }

    public override object Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new AttributeNodeRenderer();
    }
}