using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class ConditionalExpressionNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not ConditionalExpressionNode node)
        {
            throw new ArgumentException($"Expected ConditionalExpressionNode, got {nodeIn.GetType().Name}");
        }

        var condVal = renderer.Visit(node.Condition);
        // Reuse truthiness logic from BinaryExpressionNodeRenderer
        var isTrue = BinaryExpressionNodeRenderer.IsTrue(condVal);
        return isTrue ? renderer.Visit(node.TrueExpression) : renderer.Visit(node.FalseExpression);
    }
}
