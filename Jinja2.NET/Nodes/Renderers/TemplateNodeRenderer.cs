using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class TemplateNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not TemplateNode node)
        {
            throw new ArgumentException($"Expected TemplateNode, got {nodeIn.GetType().Name}");
        }

        WhitespaceTrimmer.ApplyWhitespaceTrimming(node.Children);
        foreach (var child in node.Children)
        {
            renderer.Visit(child);
        }

        return null;
    }
}