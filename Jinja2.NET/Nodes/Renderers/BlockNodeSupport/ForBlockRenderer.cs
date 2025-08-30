using System.Collections;
using System.Text;
using Jinja2.NET.Interfaces;

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
        var (loopVarNames, iterableExpr) = ExtractForBlockArguments(node);
        var items = GetIterableItems(renderer, iterableExpr);

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

    private (List<string> loopVarNames, ExpressionNode iterableExpr) ExtractForBlockArguments(BlockNode node)
    {
        // Check if the last argument is "recursive"
        bool hasRecursive = node.Arguments.LastOrDefault() is IdentifierNode { Name: "recursive" };

        // Find the position of "in"
        int expectedInPosition = hasRecursive ? node.Arguments.Count - 3 : node.Arguments.Count - 2;

        // Extract loop variable names (everything before "in")
        var loopVarNames = node.Arguments.Take(expectedInPosition)
            .OfType<IdentifierNode>()
            .Select(n => n.Name)
            .ToList();

        // Extract iterable expression (after "in", before optional "recursive")
        var iterableExpr = (ExpressionNode)node.Arguments[expectedInPosition + 1];

        return (loopVarNames, iterableExpr);
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

        // Check if the last argument is "recursive"
        var hasRecursive = node.Arguments.LastOrDefault() is IdentifierNode { Name: "recursive" };

        // Find the position of "in" - it should be before the iterable expression
        // If recursive is present: [vars...] "in" iterable "recursive"
        // If not recursive: [vars...] "in" iterable
        var expectedInPosition = hasRecursive ? node.Arguments.Count - 3 : node.Arguments.Count - 2;

        if (expectedInPosition < 1 || node.Arguments[expectedInPosition] is not IdentifierNode { Name: "in" })
        {
            throw new InvalidOperationException("'in' must come between the loop variable and the list.");
        }

        // Validate the iterable expression position
        var iterablePosition = hasRecursive ? node.Arguments.Count - 2 : node.Arguments.Count - 1;
        if (node.Arguments[iterablePosition] == null)
        {
            throw new InvalidOperationException("For loop requires an iterable expression.");
        }

        // Validate loop variable identifiers (everything before "in")
        for (var i = 0; i < expectedInPosition; i++)
            if (node.Arguments[i] is not IdentifierNode)
            {
                throw new InvalidOperationException(
                    $"Argument {i + 1} to for block must be an identifier.");
            }
    }
}