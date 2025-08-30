using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class ListLiteralNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not ListLiteralNode node)
        {
            throw new ArgumentException($"Expected ListLiteralNode, got {nodeIn.GetType().Name}");
        }

        var result = new List<object>();
        foreach (var element in node.Elements)
        {
            result.Add(renderer.Visit(element));
        }

        return result;
    }
}