using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class SetBlockRenderer : INodeRenderer
{
    public object Render(ASTNode node, IRenderer renderer)
    {
        if (node is not BlockNode setNode ||
            !setNode.Name.Equals(TemplateConstants.BlockNames.Set, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected Set BlockNode, got {node.GetType().Name}");
        }

        var identifiers = setNode.Arguments.Take(setNode.Arguments.Count - 1).OfType<IdentifierNode>().ToList();
        var valueNode = setNode.Arguments.Last();
        var value = renderer.Visit(valueNode);

        foreach (var identifier in identifiers)
        {
            var currentScope = renderer.ScopeManager.CurrentScope();

            if (currentScope.ContainsKey("loop"))
            {
                // In a loop: set only in current scope
                currentScope[identifier.Name] = value;
            }
            else if (IsAtGlobalScope(renderer.ScopeManager))
            {
                // At global level: set in both
                currentScope[identifier.Name] = value;
                renderer.Context.Set(identifier.Name, value);
            }
            else
            {
                // In an if or other block: set only in current scope
                currentScope[identifier.Name] = value;
            }
        }


        return null;
    }

    private static bool IsAtGlobalScope(IScopeManager scopeManager)
    {
        // Only one scope on the stack means global
        // Try to cast to dynamic to support any IScopeManager implementation
        try
        {
            return ((dynamic)scopeManager).ScopeDepth == 1;
        }
        catch
        {
            // Fallback: compare CurrentScope and ParentScope references
            return ReferenceEquals(scopeManager.CurrentScope(), scopeManager.ParentScope());
        }
    }
}