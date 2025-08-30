using System.Collections;
using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class IfBlockRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not BlockNode node ||
            !node.Name.Equals(TemplateConstants.BlockNames.If, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected If BlockNode, got {nodeIn.GetType().Name}");
        }

        // Evaluate the condition from the first argument
        if (node.Arguments.Count == 0)
        {
            throw new InvalidOperationException("If block requires a condition argument");
        }

        var conditionResult = renderer.Visit(node.Arguments[0]);
        var isConditionTrue = EvaluateCondition(conditionResult);

        var result = new StringBuilder();

        if (isConditionTrue)
        {
            // Render the main if block content
            RenderBlockContent(renderer, node, result);
        }
        else
        {
            // Look for elif/else blocks in children
            RenderElseOrElifBlocks(renderer, node, result);
        }

        return result.Length > 0 ? result.ToString() : null;
    }

    private static bool EvaluateCondition(object conditionResult)
    {
        return conditionResult switch
        {
            null => false,
            bool boolValue => boolValue,
            string stringValue => !string.IsNullOrEmpty(stringValue),
            int intValue => intValue != 0,
            double doubleValue => doubleValue != 0.0,
            IEnumerable enumerable => enumerable.Cast<object>().Any(),
            _ => true // Non-null objects are truthy
        };
    }

    private void RenderBlockContent(IRenderer renderer, BlockNode node, StringBuilder result)
    {
        // Apply whitespace trimming to block children
        WhitespaceTrimmer.ApplyWhitespaceTrimming(node.Children);

        // Create scope that inherits from parent scope and global context
        renderer.ScopeManager.PushScope();
        try
        {
            var currentScope = renderer.ScopeManager.CurrentScope();
            var parentScope = renderer.ScopeManager.ParentScope();

            // Copy parent scope variables to make them accessible
            foreach (var kvp in parentScope)
            {
                currentScope[kvp.Key] = kvp.Value;
            }

            foreach (var child in node.Children ?? [])
            {
                // Skip elif/else blocks when rendering main if content
                if (child is BlockNode childBlock &&
                    (childBlock.Name == TemplateConstants.BlockNames.Elif ||
                     childBlock.Name == TemplateConstants.BlockNames.Else))
                {
                    continue;
                }

                var childResult = renderer.Visit(child);
                if (childResult != null)
                {
                    result.Append(childResult);
                }
            }

            // Propagate variable changes back to parent scope
            // Only propagate variables that existed in parent scope (were modified, not newly created)
            foreach (var kvp in currentScope)
            {
                if (parentScope.ContainsKey(kvp.Key))
                {
                    parentScope[kvp.Key] = kvp.Value;
                }
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
        }
    }

    private void RenderElseOrElifBlocks(IRenderer renderer, BlockNode node, StringBuilder result)
    {
        renderer.ScopeManager.PushScope();
        try
        {
            var currentScope = renderer.ScopeManager.CurrentScope();
            var parentScope = renderer.ScopeManager.ParentScope();

            // Copy parent scope variables to make them accessible
            foreach (var kvp in parentScope)
            {
                currentScope[kvp.Key] = kvp.Value;
            }

            foreach (var child in node.Children ?? [])
            {
                if (child is not BlockNode childBlock)
                {
                    continue;
                }

                if (childBlock.Name == TemplateConstants.BlockNames.Elif)
                {
                    // Evaluate elif condition
                    if (childBlock.Arguments.Count > 0)
                    {
                        var elifCondition = renderer.Visit(childBlock.Arguments[0]);
                        if (EvaluateCondition(elifCondition))
                        {
                            // Render elif content and stop
                            RenderBlockContent(renderer, childBlock, result);
                            return;
                        }
                    }
                }
                else if (childBlock.Name == TemplateConstants.BlockNames.Else)
                {
                    // Render else content and stop
                    RenderBlockContent(renderer, childBlock, result);
                    return;
                }
            }

            // Propagate variable changes back to parent scope
            foreach (var kvp in currentScope)
            {
                if (parentScope.ContainsKey(kvp.Key))
                {
                    parentScope[kvp.Key] = kvp.Value;
                }
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
        }
    }
}