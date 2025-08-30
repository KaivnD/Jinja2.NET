using System.Collections;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class IndexNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not IndexNode node)
        {
            throw new ArgumentException($"Expected IndexNode, got {nodeIn.GetType().Name}");
        }

        var target = renderer.Visit(node.Target);
        var index = renderer.Visit(node.Index);

        // Convert double index to int if possible
        if (index is double d && d % 1 == 0)
        {
            index = (int)d;
        }

        if (target is IDictionary dict)
        {
            return dict[index];
        }

        if (target is IList list && index is int idx)
        {
            return list[idx];
        }

        if (target is Array arr)
        {
            if (index is int arrIdx)
            {
                return arr.GetValue(arrIdx);
            }

            throw new InvalidOperationException($"Array index must be int, got {index?.GetType().Name}");
        }

        if (target is string str && index is int strIdx && strIdx >= 0 && strIdx < str.Length)
        {
            return str[strIdx].ToString();
        }

        throw new InvalidOperationException($"Cannot index into type '{target?.GetType().Name}' with '{index}'.");
    }
}