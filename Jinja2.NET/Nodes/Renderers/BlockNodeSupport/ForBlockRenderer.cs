using System.Collections;
using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class ForBlockRenderer : INodeRenderer
{
    private readonly ILoopProcessorFactory _processorFactory;

    public ForBlockRenderer(ILoopProcessorFactory? processorFactory = null)
    {
        _processorFactory = processorFactory ?? new LoopProcessorFactory();
    }

    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BlockNode node ||
            !node.Name.Equals(TemplateConstants.BlockNames.For, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected For BlockNode, got {nodeIn.GetType().Name}");
        }

        ValidateForBlockArguments(node);
        var (loopVarNames, iterableExpr, ifCondition) = ExtractForBlockArguments(node);
        var items = GetIterableItems(renderer, iterableExpr);

        // If there's an 'if' condition on the for, filter the items by evaluating the condition with loop variables bound
        if (ifCondition != null)
        {
            var filtered = new List<object>();
            for (var i = 0; i < items.Count; i++)
            {
                renderer.ScopeManager.PushScope();
                try
                {
                    // copy parent scope values into current scope
                    var parent = renderer.ScopeManager.ParentScope();
                    var current = renderer.ScopeManager.CurrentScope();
                    foreach (var kvp in parent)
                    {
                        current[kvp.Key] = kvp.Value;
                    }

                    // set loop variables for this candidate item
                    if (items[i] is IEnumerable enumerable && loopVarNames.Count > 1)
                    {
                        var values = enumerable.Cast<object>().ToList();
                        for (var k = 0; k < Math.Min(loopVarNames.Count, values.Count); k++)
                        {
                            current[loopVarNames[k]] = values[k];
                        }
                    }
                    else
                    {
                        current[loopVarNames[0]] = items[i];
                    }

                    // set loop context object
                    current["loop"] = new LoopProcessor.LoopContext
                    {
                        index0 = i,
                        index = i + 1,
                        first = i == 0,
                        last = i == items.Count - 1,
                        length = items.Count,
                        previtem = i > 0 ? items[i - 1] : null
                    };

                    var condVal = renderer.Visit(ifCondition);
                    if (BinaryExpressionNodeRenderer.IsTrue(condVal))
                    {
                        filtered.Add(items[i]);
                    }
                }
                finally
                {
                    renderer.ScopeManager.PopScope();
                }
            }

            items = filtered;
        }

        var result = new StringBuilder();

        if (items.Count > 0)
        {
            var variablesBeingSet = GetVariablesBeingSet(node);
            var processor = _processorFactory.CreateProcessor(node, renderer.ScopeManager);

            // Use explicit typing to avoid var inference issues
            var loopOutput = processor.Process(renderer, node, loopVarNames, items, variablesBeingSet);
            if (!string.IsNullOrEmpty(loopOutput))
            {
                result.Append(loopOutput);
            }
        }
        else
        {
            // Now RenderElseBlock returns string
            var elseOutput = RenderElseBlock(renderer, node);
            if (!string.IsNullOrEmpty(elseOutput))
            {
                result.Append(elseOutput);
            }
        }

        return result.Length > 0 ? result.ToString() : null;
    }

    private (List<string> loopVarNames, ExpressionNode iterableExpr, ExpressionNode? ifCondition) ExtractForBlockArguments(BlockNode node)
    {
        // Find the position of "in"
        var inIndex = node.Arguments.FindIndex(a => a is IdentifierNode id && id.Name == "in");
        if (inIndex < 0)
        {
            throw new InvalidOperationException("'in' must come between the loop variable and the list.");
        }

        // Extract loop variable names (everything before "in")
        var loopVarNames = node.Arguments.Take(inIndex)
            .OfType<IdentifierNode>()
            .Select(n => n.Name)
            .ToList();

        // Extract iterable expression (the item after 'in')
        var iterableExpr = (ExpressionNode)node.Arguments[inIndex + 1];

        // Optional 'if' condition: if next token after iterable is Identifier 'if', then condition follows
        ExpressionNode? condition = null;
        var nextIndex = inIndex + 2;
        if (nextIndex < node.Arguments.Count && node.Arguments[nextIndex] is IdentifierNode id && id.Name == "if")
        {
            var condIndex = nextIndex + 1;
            if (condIndex < node.Arguments.Count)
            {
                condition = node.Arguments[condIndex];
            }
        }

        return (loopVarNames, iterableExpr, condition);
    }

    private List<object> GetIterableItems(IRenderer renderer, ExpressionNode iterableExpr)
    {
        var iterable = renderer.Visit(iterableExpr);
        if (iterable is not IEnumerable enumerable)
        {
            return Enumerable.Empty<object>().ToList();
        }

        return enumerable.Cast<object>().ToList();
    }

    private HashSet<string> GetVariablesBeingSet(BlockNode blockNode)
    {
        var setVariables = new HashSet<string>();

        void AnalyzeNode(ASTNode node)
        {
            if (node is BlockNode block)
            {
                if (block.Name == TemplateConstants.BlockNames.Set)
                {
                    foreach (var arg in block.Arguments.OfType<IdentifierNode>().Take(block.Arguments.Count - 1))
                    {
                        setVariables.Add(arg.Name);
                    }
                }

                if (block.Children != null)
                {
                    foreach (var child in block.Children)
                    {
                        AnalyzeNode(child);
                    }
                }
            }
        }

        if (blockNode.Children != null)
        {
            foreach (var child in blockNode.Children)
            {
                AnalyzeNode(child);
            }
        }

        return setVariables;
    }

    private string RenderElseBlock(IRenderer renderer, BlockNode node)
    {
        var result = new StringBuilder();
        var elseBlock = node.Children.OfType<BlockNode>()
            .FirstOrDefault(b => b.Name == TemplateConstants.BlockNames.Else);
        if (elseBlock != null)
        {
            foreach (var child in elseBlock.Children)
            {
                var childOutput = renderer.Visit(child);
                if (childOutput != null)
                {
                    result.Append(childOutput);
                }
            }
        }

        return result.ToString();
    }

    private void ValidateForBlockArguments(BlockNode node)
    {
        if (node.Arguments == null || node.Arguments.Count < 3)
        {
            throw new InvalidOperationException(
                "For block requires at least 3 arguments: 'identifier(s) in expression'.");
        }

        // Find the position of 'in'
        var inIndex = node.Arguments.FindIndex(a => a is IdentifierNode id && id.Name == "in");
        if (inIndex < 1)
        {
            throw new InvalidOperationException("'in' must come between the loop variable and the list.");
        }

        // Validate iterable presence
        if (inIndex + 1 >= node.Arguments.Count || node.Arguments[inIndex + 1] == null)
        {
            throw new InvalidOperationException("For loop requires an iterable expression.");
        }

        // Validate loop variable identifiers (everything before 'in')
        for (var i = 0; i < inIndex; i++)
            if (node.Arguments[i] is not IdentifierNode)
            {
                throw new InvalidOperationException(
                    $"Argument {i + 1} to for block must be an identifier.");
            }
    }
}