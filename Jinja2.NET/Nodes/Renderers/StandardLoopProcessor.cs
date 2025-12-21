using System.Diagnostics;
using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class StandardLoopProcessor : LoopProcessor
{
    public override string Process(IRenderer renderer, BlockNode node, List<string> loopVarNames, List<object> items,
        HashSet<string> setVariables)
    {
        var directSetVariables = GetDirectSetVariablesPrivate(node);
        var result = new StringBuilder();

        for (var i = 0; i < items.Count; i++)
        {
            // Push a fresh scope for each iteration to avoid leaking variables between iterations
            renderer.ScopeManager.PushScope();
            try
            {
                SetLoopVariables(renderer, loopVarNames, items[i], items, i);
                var loopBodyOutput = RenderLoopBody(renderer, node);
                if (!string.IsNullOrEmpty(loopBodyOutput))
                {
                    result.Append(loopBodyOutput);
                }

                // After this iteration, promote any variables that should survive to the parent scope
                var currentScope = renderer.ScopeManager.CurrentScope();
                var parentScope = renderer.ScopeManager.ParentScope();

                foreach (var variable in setVariables)
                {
                    if (loopVarNames.Contains(variable))
                    {
                        continue;
                    }

                    if (directSetVariables.Contains(variable))
                    {
                        continue;
                    }

                    if (currentScope.TryGetValue(variable, out var value))
                    {
                        parentScope[variable] = value;
                        renderer.Context.Set(variable, value);
                    }
                }
            }
            finally
            {
                renderer.ScopeManager.PopScope();
            }
        }

        return result.ToString();
    }

    protected override void PropagateVariables(IRenderer renderer, BlockNode node, List<string> loopVarNames,
        HashSet<string> setVariables)
    {
    }

    private static HashSet<string> GetDirectSetVariablesPrivate(BlockNode node)
    {
        var direct = new HashSet<string>();
        if (node.Children == null)
        {
            return direct;
        }

        foreach (var child in node.Children)
        {
            // Only consider direct set blocks, not nested blocks
            if (child is BlockNode b &&
                b.Name == TemplateConstants.BlockNames.Set)
            {
                foreach (var id in b.Arguments
                             .Take(b.Arguments.Count - 1)
                             .OfType<IdentifierNode>())
                {
                    direct.Add(id.Name);
                }
            }
        }

        return direct;
    }
}