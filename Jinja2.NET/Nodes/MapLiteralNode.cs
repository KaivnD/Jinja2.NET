using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes;

public class MapLiteralNode : ExpressionNode, IVisitable
{
    public List<KeyValuePair<ExpressionNode, ExpressionNode>> Entries { get; } = new();

    public MapLiteralNode(List<KeyValuePair<ExpressionNode, ExpressionNode>> entries)
    {
        Entries.AddRange(entries);
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public INodeRenderer GetRenderer()
    {
        return new Nodes.Renderers.MapLiteralNodeRenderer();
    }
}
