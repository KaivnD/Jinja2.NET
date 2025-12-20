using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

/// <summary>
/// 一元表达式节点，用于承载 not 等只有单个操作数的运算符
/// 与 BinaryExpressionNode（二元）保持一致的接口实现风格
/// </summary>
public class UnaryExpressionNode : ExpressionNode, IVisitable
{
    /// <summary>
    /// 一元运算符（如 "not"）
    /// </summary>
    public string Operator { get; }

    /// <summary>
    /// 运算操作数（一元表达式只有单个操作数）
    /// </summary>
    public ExpressionNode Operand { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="op">一元运算符</param>
    /// <param name="operand">运算操作数</param>
    public UnaryExpressionNode(string op, ExpressionNode operand)
    {
        Operator = op;
        Operand = operand;
    }

    /// <summary>
    /// 接受访问者（符合 IVisitable 接口规范，与 BinaryExpressionNode 一致）
    /// </summary>
    /// <param name="visitor">节点访问者</param>
    /// <returns>访问结果</returns>
    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    /// <summary>
    /// 获取当前节点对应的渲染器（与 BinaryExpressionNode 一致）
    /// </summary>
    /// <returns>UnaryExpressionNodeRenderer 实例</returns>
    public INodeRenderer GetRenderer()
    {
        return new UnaryExpressionNodeRenderer();
    }
}