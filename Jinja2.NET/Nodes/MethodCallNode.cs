using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes;

public class MethodCallNode : ExpressionNode
{
    public ExpressionNode Object { get; }
    public string MethodName { get; }
    public List<ExpressionNode> Arguments { get; }
    public Dictionary<string, ExpressionNode> Kwargs { get; }

    public MethodCallNode(ExpressionNode obj, string methodName, List<ExpressionNode> arguments, Dictionary<string, ExpressionNode>? kwargs = null)
    {
        Object = obj ?? throw new ArgumentNullException(nameof(obj));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        Arguments = arguments ?? new List<ExpressionNode>();
        Kwargs = kwargs ?? new Dictionary<string, ExpressionNode>();
    }

    public override object? Accept(INodeVisitor visitor)
    {
        return visitor.Visit(this);
    }

    public override string ToString()
    {
        var args = Arguments.Select(arg => arg.ToString()).ToList();
        if (Kwargs.Count > 0)
        {
            args.AddRange(Kwargs.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        var argsStr = args.Count > 0 ? string.Join(", ", args) : "";
        return $"MethodCallNode: {Object}.{MethodName}({argsStr})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not MethodCallNode other) return false;
        return Object.Equals(other.Object) &&
               MethodName == other.MethodName &&
               Arguments.SequenceEqual(other.Arguments) &&
               Kwargs.Count == other.Kwargs.Count &&
               Kwargs.All(k => other.Kwargs.ContainsKey(k.Key) && Equals(other.Kwargs[k.Key], k.Value));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Object, MethodName, Arguments.Count, Kwargs.Count);
    }
}
