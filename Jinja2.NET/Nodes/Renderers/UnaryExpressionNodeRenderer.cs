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
            "-" => Negate(operand),
            "+" => Plus(operand),
            _ => throw new InvalidOperationException($"Unsupported unary operator: {node.Operator}")
        };
    }

    private static object? Plus(object? operand)
    {
        if (operand is int || operand is double || operand is float || operand is long || operand is decimal)
        {
            return operand;
        }
         
        if (operand != null && double.TryParse(operand.ToString(), out var result))
        {
             if (result % 1 == 0) return (int)result;
             return result;
        }
        
        throw new InvalidOperationException($"Cannot apply unary plus to {operand?.GetType().Name ?? "null"}");
    }

    private static object? Negate(object? operand)
    {
        if (operand is int i) return -i;
        if (operand is double d) return -d;
        if (operand is float f) return -f;
        if (operand is long l) return -l;
        if (operand is decimal m) return -m;
        
        if (operand != null && double.TryParse(operand.ToString(), out var result))
        {
             if (result % 1 == 0) return -(int)result;
             return -result;
        }

        throw new InvalidOperationException($"Cannot negate {operand?.GetType().Name ?? "null"}");
    }

    // 一元布尔非运算：对操作数的真值取反
    private static bool Not(object? operand)
    {
        return !BinaryExpressionNodeRenderer.IsTrue(operand); // 复用已有的 IsTrue 方法
    }
}