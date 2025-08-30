using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class TextNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not TextNode node)
        {
            throw new ArgumentException($"Expected TextNode, got {nodeIn.GetType().Name}");
        }

        var content = node.Content;

        if (node.TrimLeft)
        {
            content = content.TrimStart(' ', '\t', '\r', '\n');
        }

        if (node.TrimRight)
        {
            content = content.TrimEnd(' ', '\t', '\r', '\n');
        }

        return content;
    }
}