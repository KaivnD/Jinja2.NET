using System.Collections;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class VariableNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not VariableNode node)
        {
            throw new ArgumentException($"Expected VariableNode, got {nodeIn.GetType().Name}");
        }

        var value = renderer.Visit(node.Expression);

        if (value != null)
        {
            if (value is IList list && value is not string)
            {
                var items = new List<string>();
                foreach (var item in list)
                {
                    items.Add(item?.ToString() ?? "null");
                }

                return $"[{string.Join(", ", items)}]";
            }

            // ✅ Fix 3: Return the value instead of writing to Output
            return value.ToString();
        }

        return null; // Return null for null values
    }
}