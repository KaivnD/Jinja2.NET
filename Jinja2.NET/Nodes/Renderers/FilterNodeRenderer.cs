using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class FilterNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not FilterNode node)
        {
            throw new ArgumentException($"Expected FilterNode, got {nodeIn.GetType().Name}");
        }

        var value = renderer.Visit(node.Expression);
        var args = node.Arguments.Select(arg => renderer.Visit(arg)).ToArray();

        if (renderer.CustomFilters != null && renderer.CustomFilters.TryGetValue(node.FilterName, out var customFilter))
        {
            return customFilter(value, args);
        }

        if (BuiltinFilters.HasFilter(node.FilterName))
        {
            return BuiltinFilters.ApplyFilter(node.FilterName, value, args);
        }

        throw new InvalidOperationException($"Unknown filter: {node.FilterName}");
    }
}