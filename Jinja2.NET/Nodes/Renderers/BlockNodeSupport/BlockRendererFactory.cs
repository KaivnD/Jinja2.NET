using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class BlockRendererFactory
{
    private readonly INodeRenderer _defaultRenderer;
    private readonly Dictionary<string, INodeRenderer> _renderers; // ✅ Use INodeRenderer

    public BlockRendererFactory()
    {
        _renderers = new Dictionary<string, INodeRenderer> // ✅ Use INodeRenderer
        {
            { TemplateConstants.BlockNames.For, new ForBlockRenderer() },
            { TemplateConstants.BlockNames.If, new IfBlockRenderer() },
            { TemplateConstants.BlockNames.Raw, new RawBlockRenderer() },
            { TemplateConstants.BlockNames.Set, new SetBlockRenderer() }
        };
        _defaultRenderer = new DefaultBlockRenderer();
    }

    public INodeRenderer GetRenderer(string blockName) // ✅ Return INodeRenderer
    {
        return _renderers.TryGetValue(blockName.ToLower(), out var renderer)
            ? renderer
            : _defaultRenderer;
    }
}