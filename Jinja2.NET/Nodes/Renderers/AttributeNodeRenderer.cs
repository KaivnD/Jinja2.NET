using System.Collections;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class AttributeNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode node, IRenderer renderer)
    {
        if (node is not AttributeNode attrNode)
        {
            throw new ArgumentException($"Expected AttributeNode, got {node.GetType().Name}");
        }

        var obj = renderer.Visit(attrNode.Object);
        if (obj == null)
        {
            return null;
        }

        var property = obj.GetType().GetProperty(attrNode.Attribute);
        if (property != null)
        {
            return property.GetValue(obj);
        }

        if (obj is IDictionary dict && dict.Contains(attrNode.Attribute))
        {
            return dict[attrNode.Attribute];
        }

        return null;
    }
}