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
        renderer.ScopeManager.PushScope(); // Push once for the whole loop
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                SetLoopVariables(renderer, loopVarNames, items[i], items, i);
                var loopBodyOutput = RenderLoopBody(renderer, node);
                if (!string.IsNullOrEmpty(loopBodyOutput))
                {
                    result.Append(loopBodyOutput);
                }
            }

            // After all iterations, promote variables to parent (global) scope if they were not set directly here
            var currentScope = renderer.ScopeManager.CurrentScope();
            var parentScope = renderer.ScopeManager.ParentScope();

            Trace.WriteLine($"[DEBUG] After outer loop, x = {(currentScope.ContainsKey("x") ? currentScope["x"] : "<none>")}");
            Trace.WriteLine($"[DEBUG] setVariables: {string.Join(",", setVariables)}");
            Trace.WriteLine($"[DEBUG] directSetVariables: {string.Join(",", directSetVariables)}");

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
                    // Promote into parent scope (so {{ x }} after loop sees it)
                    parentScope[variable] = value;
                    // Keep context in sync (optional)
                    renderer.Context.Set(variable, value);
                }
            }

            if (currentScope.ContainsKey("x"))
            {
                Trace.WriteLine($"[DEBUG] After outer loop, x = {currentScope["x"]}");
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
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