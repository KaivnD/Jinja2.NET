// TemplateNode.cs

using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class TemplateNode : ASTNode, IVisitable
{
    public List<ASTNode> Children { get; } = new();

    public TemplateNode()
    {
    }

    public TemplateNode(IEnumerable<ASTNode> children)
    {
        if (children != null)
        {
            Children.AddRange(children);
        }
    }

    public override object Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new TemplateNodeRenderer();
    }
}