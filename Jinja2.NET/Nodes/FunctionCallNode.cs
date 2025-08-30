using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes;

public class FunctionCallNode : ExpressionNode
{
    public string FunctionName { get; }
    public List<ExpressionNode> Arguments { get; }

    public FunctionCallNode(string functionName, List<ExpressionNode> arguments)
    {
        FunctionName = functionName ?? throw new ArgumentNullException(nameof(functionName));
        Arguments = arguments ?? new List<ExpressionNode>();
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString()
    {
        var args = Arguments.Count > 0
            ? string.Join(", ", Arguments.Select(arg => arg.ToString()))
            : "";
        return $"FunctionCallNode: {FunctionName}({args})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionCallNode other) return false;
        return FunctionName == other.FunctionName &&
               Arguments.SequenceEqual(other.Arguments);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FunctionName, Arguments.Count);
    }
}