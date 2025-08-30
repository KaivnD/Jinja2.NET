using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class RawNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode node, IRenderer renderer)
    {
        if (node is not RawNode rawNode)
        {
            throw new InvalidOperationException($"Expected RawNode, got {node.GetType().Name}");
        }

        var content = rawNode.Content;
        if (rawNode.StartMarkerType == ETokenType.BlockStart &&
            rawNode.EndMarkerType == ETokenType.BlockEnd &&
            rawNode.Content != null)
        {
            content = rawNode.Content.Trim();
        }

        return content;
    }
}