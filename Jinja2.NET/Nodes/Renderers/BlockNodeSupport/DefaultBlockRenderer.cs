using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class DefaultBlockRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BlockNode node)
            throw new ArgumentException($"Expected BlockNode, got {nodeIn.GetType().Name}");

        var result = new StringBuilder(); // Collect output

        // Render children and collect results
        foreach (var child in node.Children ?? [])
        {
            var childResult = renderer.Visit(child); //  Capture result
            if (childResult != null)
            {
                result.Append(childResult); //  Append to output
            }
        }

        return result.Length > 0 ? result.ToString() : null; //  Return collected output
    }
}