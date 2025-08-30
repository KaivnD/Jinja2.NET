using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class RecursiveLoopProcessor : LoopProcessor
{
    public override string Process(IRenderer renderer, BlockNode node, List<string> loopVarNames, List<object> items,
        HashSet<string> setVariables)
    {
        var result = new StringBuilder();

        renderer.ScopeManager.PushScope();
        try
        {
            // Store the template reference for recursive calls at the scope level
            var currentScope = renderer.ScopeManager.CurrentScope();
            currentScope["__recursive_template__"] = node;

            for (var i = 0; i < items.Count; i++)
            {
                SetLoopVariables(renderer, loopVarNames, items[i], items, i);

                var loopBodyOutput = RenderLoopBody(renderer, node);
                if (loopBodyOutput != null)
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

    protected override void PropagateVariables(IRenderer renderer, BlockNode node, List<string> loopVarNames,
        HashSet<string> setVariables)
    {
        // Recursive loops should not propagate variables to avoid scope pollution
        // This maintains proper isolation between recursive calls
    }
}