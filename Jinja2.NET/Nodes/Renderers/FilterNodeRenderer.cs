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
        var argsList = node.Arguments.Select(arg => renderer.Visit(arg)).ToList();

        if (node.Kwargs != null && node.Kwargs.Count > 0)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kv in node.Kwargs)
            {
                dict[kv.Key] = renderer.Visit(kv.Value);
            }
            // Append kwargs dictionary as a single last argument
            argsList.Add(dict);
        }

        var args = argsList.ToArray();

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