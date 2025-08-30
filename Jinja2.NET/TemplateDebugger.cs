using System.Text;
using Jinja2.NET.Nodes;

namespace Jinja2.NET;

public static class TemplateDebugger
{
    private static readonly Dictionary<Type, Action<StringBuilder, ASTNode, int>> _nodePrinters = new()
    {
        [typeof(TemplateNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}TemplateNode");
            foreach (var child in ((TemplateNode)node).Children)
            {
                PrintNode(sb, child, indent + 1);
            }
        },
        [typeof(BlockNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            var block = (BlockNode)node;
            var extra = block.IsLoopScoped ? " (IsLoopScoped = true)" : "";
            sb.AppendLine($"{pad}BlockNode: {EscapeNonPrintable(block.Name)}{extra}");

            // Pretty-print arguments if any
            if (block.Arguments.Count > 0)
            {
                sb.AppendLine($"{pad}  Arguments:");
                foreach (var arg in block.Arguments)
                {
                    switch (arg)
                    {
                        case IdentifierNode id:
                            sb.AppendLine($"{pad}    - IdentifierNode: \"{EscapeNonPrintable(id.Name)}\"");
                            break;
                        case LiteralNode lit:
                            sb.AppendLine($"{pad}    - LiteralNode: {EscapeNonPrintable(lit.Value?.ToString() ?? "")}");
                            break;
                        default:
                            // Pretty-print complex nodes (e.g., BinaryExpressionNode) with proper indentation
                            sb.Append($"{pad}    - ");
                            PrintNode(sb, arg, indent + 2);
                            break;
                    }
                }

                sb.AppendLine($"{pad}  End Arguments--------");
            }

            foreach (var child in block.Children)
            {
                PrintNode(sb, child, indent + 1);
            }
        },
        [typeof(BinaryExpressionNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            var bin = (BinaryExpressionNode)node;
            sb.AppendLine($"{pad}BinaryExpressionNode: \"{EscapeNonPrintable(bin.Operator)}\"");
            sb.AppendLine($"{pad}  Left:");
            PrintNode(sb, bin.Left, indent + 2);
            sb.AppendLine($"{pad}  Right:");
            PrintNode(sb, bin.Right, indent + 2);
        },
        [typeof(TextNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}TextNode: \"{EscapeNonPrintable(((TextNode)node).Content)}\"");
        },
        [typeof(VariableNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            var varNode = (VariableNode)node;
            sb.AppendLine($"{pad}VariableNode:");
            if (varNode.Expression != null)
            {
                PrintNode(sb, varNode.Expression, indent + 1);
            }
        },
        [typeof(FilterNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}FilterNode: {EscapeNonPrintable(((FilterNode)node).FilterName)}");
        },
        [typeof(IdentifierNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}IdentifierNode: {EscapeNonPrintable(((IdentifierNode)node).Name)}");
        },
        [typeof(LiteralNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}LiteralNode: {EscapeNonPrintable(((LiteralNode)node).Value?.ToString() ?? "")}");
        },
        [typeof(ListLiteralNode)] = (sb, node, indent) =>
        {
            var pad = new string(' ', indent * 2);
            var listNode = (ListLiteralNode)node;
            sb.AppendLine($"{pad}ListLiteralNode");
            foreach (var element in listNode.Elements)
            {
                PrintNode(sb, element, indent + 1);
            }
        }
    };

    public static string DebugAst(string header, TemplateNode ast)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        PrintNode(sb, ast, 0);
        return sb.ToString();
    }

    public static string DebugTokens(string header, IEnumerable<Token> tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var token in tokens)
        {
            var value = EscapeNonPrintable(token.Value);
            sb.AppendLine($"Token: {token.Type} [{value}]");
        }

        return sb.ToString();
    }

    public static string GetExtendedTokensInfo(string headerText, List<Token> tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine(headerText);
        for (var i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            var value = EscapeNonPrintable(t.Value);
            sb.AppendLine(
                $"{i}: {t.Type} \"{value}\" TrimLeft={t.TrimLeft} TrimRight={t.TrimRight}, Line:{t.Line} Col:{t.Column}");
        }

        return sb.ToString();
    }

    private static string EscapeNonPrintable(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsControl(c) || c == 127) // ASCII control characters (0-31, 127)
            {
                switch (c)
                {
                    case '\0': sb.Append("\\0"); break;
                    case '\a': sb.Append("\\a"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\v': sb.Append("\\v"); break;
                    default:
                        sb.Append($"\\u{(int)c:D4}"); // Use \uXXXX for other control characters
                        break;
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static void PrintNode(StringBuilder sb, ASTNode node, int indent)
    {
        if (_nodePrinters.TryGetValue(node.GetType(), out var printer))
        {
            printer(sb, node, indent);
        }
        else
        {
            var pad = new string(' ', indent * 2);
            sb.AppendLine($"{pad}{node.GetType().Name}");
        }
    }
}