using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class LiteralNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not LiteralNode node)
        {
            throw new ArgumentException($"Expected LiteralNode, got {nodeIn.GetType().Name}");
        }

        // Just return the value of the literal node
        return node.Value;
    }
}