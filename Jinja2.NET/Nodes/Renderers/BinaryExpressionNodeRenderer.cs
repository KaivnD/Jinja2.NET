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

        // Implement short-circuit evaluation for 'and' / 'or'
        if (node.Operator.Equals("and", StringComparison.OrdinalIgnoreCase))
        {
            var leftVal = renderer.Visit(node.Left);
            if (!IsTrue(leftVal)) return false;
            var rightVal = renderer.Visit(node.Right);
            return IsTrue(rightVal);
        }

        if (node.Operator.Equals("or", StringComparison.OrdinalIgnoreCase))
        {
            var leftVal = renderer.Visit(node.Left);
            if (IsTrue(leftVal)) return true;
            var rightVal = renderer.Visit(node.Right);
            return IsTrue(rightVal);
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
            "is" => HandleIsOperator(left, node.Right, renderer),
            "and" => And(left, right),
            "or" => Or(left, right),
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

        // If either side is a string, coerce both to string and concatenate
        if (left is string || right is string)
        {
            return (left?.ToString() ?? string.Empty) + (right?.ToString() ?? string.Empty);
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

        // If both can be coerced to numbers, compare numerically to avoid
        // double vs int boxed equality returning false (e.g. 0.0 vs 0).
        if (TryCoerceToNumber(left, right, out var l, out var r))
        {
            return Math.Abs(l - r) < Double.Epsilon;
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

        if (right is string str && left is string s)
        {
            return str.Contains(s);
        }

        if (right is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                // Non-generic IDictionary iteration yields DictionaryEntry
                if (item is System.Collections.DictionaryEntry de)
                {
                    if (EqualsPrivate(left, de.Key))
                    {
                        return true;
                    }
                    continue;
                }

                // Generic dictionaries iterate KeyValuePair<TKey,TValue>
                if (item != null)
                {
                    var itemType = item.GetType();
                    if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
                    {
                        var keyProp = itemType.GetProperty("Key");
                        var key = keyProp?.GetValue(item);
                        if (EqualsPrivate(left, key))
                        {
                            return true;
                        }
                        continue;
                    }
                }

                if (EqualsPrivate(left, item))
                {
                    return true;
                }
            }

            return false;
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
            "string" => left is string,
            _ => throw new InvalidOperationException($"Unsupported test: {testName}")
        };
    }

    private static bool And(object? left, object? right)
    {
        if (!IsTrue(left))
        {
            return false;
        }
        return IsTrue(right);
    }

    private static bool Or(object? left, object? right)
    {
        if (IsTrue(left))
        {
            return true;
        }
        return IsTrue(right);
    }

    internal static bool IsTrue(object? value)
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

    private static bool HandleIsOperator(object? left, ASTNode rightNode, IRenderer renderer)
    {
        // Support LiteralNode boolean (e.g. parsed 'false'/'true' became LiteralNode(false))
        if (rightNode is LiteralNode lit && lit.Value is bool litBool)
        {
            return Is(left, litBool ? "true" : "false");
        }

        // If the right side is an identifier (e.g. 'defined'), use its name as the test
        if (rightNode is IdentifierNode idNode)
        {
            return Is(left, idNode.Name);
        }

        // Handle 'is not foo' where right side is a unary 'not' operator applied to an identifier or literal
        if (rightNode is UnaryExpressionNode unary && unary.Operator.Equals("not", StringComparison.OrdinalIgnoreCase))
        {
            // operand is identifier -> invert test
            if (unary.Operand is IdentifierNode idOperand)
            {
                return !Is(left, idOperand.Name);
            }

            // operand evaluates to string test name
            var operandVal = renderer.Visit(unary.Operand);
            if (operandVal is string s)
            {
                return !Is(left, s);
            }

            // operand evaluates to boolean literal -> invert corresponding test
            if (operandVal is bool b)
            {
                return !Is(left, b ? "true" : "false");
            }

            throw new InvalidOperationException($"Right operand of 'is' must be a test name, got {operandVal?.GetType()}");
        }

        // Fallback: evaluate right and accept string or boolean
        var rightVal = renderer.Visit(rightNode);
        if (rightVal is string testName)
        {
            return Is(left, testName);
        }

        if (rightVal is bool boolVal)
        {
            return Is(left, boolVal ? "true" : "false");
        }

        throw new InvalidOperationException($"Right operand of 'is' must be a test name, got {rightVal?.GetType()}");
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