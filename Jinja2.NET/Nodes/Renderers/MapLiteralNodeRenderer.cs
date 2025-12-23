using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class MapLiteralNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not MapLiteralNode node)
        {
            throw new ArgumentException($"Expected MapLiteralNode, got {nodeIn.GetType().Name}");
        }

        var result = new Dictionary<object, object?>();
        foreach (var kv in node.Entries)
        {
            var keyVal = renderer.Visit(kv.Key);
            var val = renderer.Visit(kv.Value);
            result[keyVal ?? ""] = val;
        }

        return result;
    }
}
