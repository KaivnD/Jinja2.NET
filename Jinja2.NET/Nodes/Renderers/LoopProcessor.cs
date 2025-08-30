using System.Collections;
using System.Diagnostics;
using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public abstract class LoopProcessor : ILoopProcessor
{
    public class LoopContext
    {
        public bool first { get; set; }
        public int index { get; set; }
        public int index0 { get; set; }
        public bool last { get; set; }
        public int length { get; set; }
    }

    public virtual string Process(IRenderer renderer, BlockNode node, List<string> loopVarNames, List<object> items,
        HashSet<string> setVariables)
    {
        var result = new StringBuilder();

        renderer.ScopeManager.PushScope();
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

        renderer.ScopeManager.PopScope();
        return result.ToString();
    }

    // Variables set directly in this loop body (ignores nested blocks)
    protected static HashSet<string> GetDirectSetVariables(BlockNode node)
    {
        var direct = new HashSet<string>();
        if (node.Children == null)
        {
            return direct;
        }

        foreach (var child in node.Children)
        {
            if (child is BlockNode b && b.Name == TemplateConstants.BlockNames.Set)
            {
                foreach (var id in b.Arguments.Take(b.Arguments.Count - 1).OfType<IdentifierNode>())
                {
                    direct.Add(id.Name);
                }
            }
        }

        return direct;
    }

    protected virtual void PropagateVariables(IRenderer renderer, BlockNode node, List<string> loopVarNames,
        HashSet<string> setVariables)
    {
    }

    protected string RenderLoopBody(IRenderer renderer, BlockNode node)
    {
        var result = new StringBuilder();
        WhitespaceTrimmer.ApplyWhitespaceTrimming(node.Children);

        foreach (var child in node.Children)
        {
            if (child is BlockNode { Name: TemplateConstants.BlockNames.Else })
            {
                break;
            }

            var childOutput = renderer.Visit(child); // Collect the returned content
            if (childOutput != null)
            {
                result.Append(childOutput);
            }
        }

        return result.ToString();
    }

    protected void SetLoopVariables(IRenderer renderer, List<string> loopVarNames, object item, List<object> items,
        int index)
    {
        var currentScope = renderer.ScopeManager.CurrentScope();
        if (item is IEnumerable enumerable && loopVarNames.Count > 1)
        {
            var values = enumerable.Cast<object>().ToList();
            for (var i = 0; i < Math.Min(loopVarNames.Count, values.Count); i++)
            {
                currentScope[loopVarNames[i]] = values[i];
                Trace.WriteLine($"TRACE: Set loop variable {loopVarNames[i]} = {values[i]}");
            }
        }
        else
        {
            currentScope[loopVarNames[0]] = item;
            Trace.WriteLine($"TRACE: Set loop variable {loopVarNames[0]} = {item}");
        }

        // Create a simple object that can be accessed via attribute notation
        currentScope["loop"] = new LoopContext
        {
            index0 = index,
            index = index + 1,
            first = index == 0,
            last = index == items.Count - 1,
            length = items.Count
        };
    }
}