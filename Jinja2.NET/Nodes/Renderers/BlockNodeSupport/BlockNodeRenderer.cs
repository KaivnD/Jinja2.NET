using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class BlockNodeRenderer : INodeRenderer
{
    private readonly BlockRendererFactory _factory;

    public BlockNodeRenderer()
    {
        _factory = new BlockRendererFactory();
    }

    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BlockNode node)
        {
            throw new ArgumentException($"Expected BlockNode, got {nodeIn.GetType().Name}");
        }

        var rendererInstance = _factory.GetRenderer(node.Name);
        return rendererInstance.Render(node, renderer);
    }

   
}