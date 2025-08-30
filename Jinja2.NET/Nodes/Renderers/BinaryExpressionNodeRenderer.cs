using System.Collections;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class BinaryExpressionNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BinaryExpressionNode node)
        {
            throw new ArgumentException($"Expected BinaryExpressionNode, got {nodeIn.GetType().Name}");
        }

        var left = renderer.Visit(node.Left);
        var right = renderer.Visit(node.Right);

        return node.Operator switch
        {
            "+" => Add(left, right),
            "-" => Subtract(left, right),
            "*" => Multiply(left, right),
            "/" => Divide(left, right),
            "//" => FloorDivide(left, right),
            "%" => Modulo(left, right),
            "==" => EqualsPrivate(left, right),
            "!=" => !EqualsPrivate(left, right),
            "<" => Compare(left, right) < 0,
            ">" => Compare(left, right) > 0,
            "<=" => Compare(left, right) <= 0,
            ">=" => Compare(left, right) >= 0,
            "in" => In(left, right),
            "is" => Is(left, right),
            _ => throw new InvalidOperationException($"Unsupported operator: {node.Operator}")
        };
    }

    private static object? Add(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return l + r;
        }

        if (left is string ls && right is string rs)
        {
            return ls + rs;
        }

        if (left is IEnumerable leftList && right is IEnumerable rightList)
        {
            return ConcatLists(leftList, rightList);
        }

        throw new InvalidOperationException($"Cannot add {left.GetType()} and {right.GetType()}");
    }

    private static int Compare(object? left, object? right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return -1;
        }

        if (right == null)
        {
            return 1;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return l.CompareTo(r);
        }

        if (left is string ls && right is string rs)
        {
            return string.Compare(ls, rs, StringComparison.Ordinal);
        }

        throw new InvalidOperationException($"Cannot compare {left.GetType()} and {right.GetType()}");
    }

    private static IEnumerable ConcatLists(IEnumerable left, IEnumerable right)
    {
        var result = new List<object>();
        foreach (var item in left)
        {
            result.Add(item);
        }

        foreach (var item in right)
        {
            result.Add(item);
        }

        return result;
    }

    private static object? Divide(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return r != 0 ? l / r : throw new DivideByZeroException();
        }

        throw new InvalidOperationException($"Cannot divide {left.GetType()} and {right.GetType()}");
    }

    private static bool EqualsPrivate(object? left, object? right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        return object.Equals(left, right);
    }

    private static object? FloorDivide(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return r != 0 ? Math.Floor(l / r) : throw new DivideByZeroException();
        }

        throw new InvalidOperationException($"Cannot floor divide {left.GetType()} and {right.GetType()}");
    }

    private static bool In(object left, object? right)
    {
        if (right == null)
        {
            return false;
        }

        if (right is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (EqualsPrivate(left, item))
                {
                    return true;
                }
            }

            return false;
        }

        if (right is string str && left is string s)
        {
            return str.Contains(s);
        }

        throw new InvalidOperationException($"Cannot perform 'in' with {left?.GetType()} and {right.GetType()}");
    }

    private static bool Is(object? left, object? right) 
    {
        if (right is not string testName)
        {
            throw new InvalidOperationException($"Right operand of 'is' must be a test name, got {right?.GetType()}");
        }

        return testName.ToLower() switch
        {
            "defined" => left != null,
            "undefined" => left == null,
            "none" => left == null,
            "true" => IsTrue(left), 
            "false" => !IsTrue(left),
            _ => throw new InvalidOperationException($"Unsupported test: {testName}")
        };
    }

    private static bool IsTrue(object? value)
    {
        return value switch
        {
            null => false,
            bool boolValue => boolValue,
            string stringValue => !string.IsNullOrEmpty(stringValue),
            int intValue => intValue != 0,
            double doubleValue => doubleValue != 0.0,
            System.Collections.IEnumerable enumerable => enumerable.Cast<object>().Any(),
            _ => true // Non-null objects are truthy
        };
    }

    private static object? Modulo(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return r != 0 ? l % r : throw new DivideByZeroException();
        }

        throw new InvalidOperationException($"Cannot modulo {left.GetType()} and {right.GetType()}");
    }

    private static object? Multiply(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return l * r;
        }

        throw new InvalidOperationException($"Cannot multiply {left.GetType()} and {right.GetType()}");
    }

    private static object? Subtract(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return l - r;
        }

        throw new InvalidOperationException($"Cannot subtract {left.GetType()} and {right.GetType()}");
    }

    private static bool TryCoerceToNumber(object left, object right, out double leftNumber, out double rightNumber)
    {
        leftNumber = rightNumber = 0;

        if (left is int leftInt && right is int rightInt)
        {
            leftNumber = leftInt;
            rightNumber = rightInt;
            return true;
        }

        if (left is double leftDouble && right is double rightDouble)
        {
            leftNumber = leftDouble;
            rightNumber = rightDouble;
            return true;
        }

        if (left is int leftInt2 && right is double rightDouble2)
        {
            leftNumber = leftInt2;
            rightNumber = rightDouble2;
            return true;
        }

        if (left is double leftDouble2 && right is int rightInt2)
        {
            leftNumber = leftDouble2;
            rightNumber = rightInt2;
            return true;
        }

        if (left is string leftString && double.TryParse(leftString, out leftNumber))
        {
            if (right is string rightString && double.TryParse(rightString, out rightNumber))
            {
                return true;
            }

            if (right is int rightInt3)
            {
                rightNumber = rightInt3;
                return true;
            }

            if (right is double rightDouble3)
            {
                rightNumber = rightDouble3;
                return true;
            }
        }

        return false;
    }
}