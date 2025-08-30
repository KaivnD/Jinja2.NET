using System.Diagnostics;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class IdentifierNodeRenderer : INodeRenderer
{
    public object Render(ASTNode node, IRenderer renderer)
    {
        if (node is not IdentifierNode idNode)
        {
            throw new ArgumentException($"Expected IdentifierNode, got {node.GetType().Name}");
        }

        // First check current scope
        var currentScope = renderer.ScopeManager.CurrentScope();
        if (currentScope.TryGetValue(idNode.Name, out var value))
        {
            Trace.WriteLine($"TRACE: Identifier {idNode.Name} = {value} (from current scope)");
            return value;
        }

        // Then check parent scope
        var parentScope = renderer.ScopeManager.ParentScope();
        if (parentScope.TryGetValue(idNode.Name, out value))
        {
            Trace.WriteLine($"TRACE: Identifier {idNode.Name} = {value} (from parent scope)");
            return value;
        }

        // Finally check global context
        value = renderer.Context.Get(idNode.Name);
        Trace.WriteLine($"TRACE: Identifier {idNode.Name} = {(value != null ? value : "null")} (from TemplateContext)");
        return value;
    }
}