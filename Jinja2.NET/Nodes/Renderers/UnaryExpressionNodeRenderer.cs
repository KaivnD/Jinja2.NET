using System;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes; // 确保引入 UnaryExpressionNode 命名空间

namespace Jinja2.NET.Nodes.Renderers;

public class UnaryExpressionNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not UnaryExpressionNode node)
        {
            throw new ArgumentException($"Expected UnaryExpressionNode, got {nodeIn.GetType().Name}");
        }

        // 渲染操作数（not 只有一个操作数）
        var operand = renderer.Visit(node.Operand);

        // 处理 not 运算符
        return node.Operator switch
        {
            "not" => Not(operand),
            _ => throw new InvalidOperationException($"Unsupported unary operator: {node.Operator}")
        };
    }

    // 一元布尔非运算：对操作数的真值取反
    private static bool Not(object? operand)
    {
        return !BinaryExpressionNodeRenderer.IsTrue(operand); // 复用已有的 IsTrue 方法
    }
}