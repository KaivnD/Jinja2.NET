using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class RawBlockRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BlockNode node)
        {
            throw new ArgumentException($"Expected BlockNode, got {nodeIn.GetType().Name}");
        }

        var result = new StringBuilder();

        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                result.Append(textNode.Content);
            }
            else
            {
                var childResult = renderer.Visit(child);
                if (childResult != null)
                {
                    result.Append(childResult);
                }
            }
        }

        return result.ToString();
    }
}