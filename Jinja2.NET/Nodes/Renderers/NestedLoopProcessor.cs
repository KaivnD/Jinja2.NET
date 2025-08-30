using Jinja2.NET.Interfaces;
using System.Text;
using System.Linq;

namespace Jinja2.NET.Nodes.Renderers;

public class NestedLoopProcessor : LoopProcessor
{
    public override string Process(IRenderer renderer, BlockNode node, List<string> loopVarNames, List<object> items, HashSet<string> setVariables)
    {
        var result = new StringBuilder();
        renderer.ScopeManager.PushScope();
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

                PropagateVariables(renderer, node, loopVarNames, setVariables);
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
        }
        return result.ToString();
    }

    protected override void PropagateVariables(IRenderer renderer, BlockNode node, List<string> loopVarNames, HashSet<string> setVariables)
    {
        var current = renderer.ScopeManager.CurrentScope();
        var parent = renderer.ScopeManager.ParentScope();

        // Only propagate variables set directly in THIS loop body
        var directSetVariables = GetDirectSetVariables(node);
        foreach (var v in directSetVariables)
        {
            if (v != loopVarNames.FirstOrDefault() && current.ContainsKey(v))
            {
                parent[v] = current[v];
                System.Diagnostics.Trace.WriteLine($"[NESTED] Propagated {v}={current[v]} to parent");
            }
        }
        System.Diagnostics.Trace.WriteLine($"[NESTED] After propagation, parent x = {(parent.ContainsKey("x") ? parent["x"] : "<none>")}");
    }
}