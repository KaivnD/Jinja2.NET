using System.Collections;
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

        var targets = setNode.Arguments.Take(setNode.Arguments.Count - 1).ToList();
        object? value;

        // If the set block has children, it's the block-form: capture rendered children as the value
        if (setNode.Children != null && setNode.Children.Count > 0)
        {
            // Determine variables that will be set inside this block so they can be propagated
            var setVariables = GetVariablesBeingSet(setNode);

            // Trim whitespace for block children
            WhitespaceTrimmer.ApplyWhitespaceTrimming(setNode.Children);

            // Render children into a string within a new scope
            renderer.ScopeManager.PushScope();
            try
            {
                var currentScope = renderer.ScopeManager.CurrentScope();
                var parentScope = renderer.ScopeManager.ParentScope();

                foreach (var kvp in parentScope)
                {
                    currentScope[kvp.Key] = kvp.Value;
                }

                var sb = new System.Text.StringBuilder();
                foreach (var child in setNode.Children)
                {
                    var childResult = renderer.Visit(child);
                    if (childResult != null)
                    {
                        sb.Append(childResult);
                    }
                }

                // Propagate set variables back to parent scope if needed
                renderer.ScopeManager.PropagateVariablesToParent(renderer.Context, string.Empty, setVariables);

                var raw = sb.ToString();
                // Trim leading/trailing whitespace from captured content
                raw = raw.Trim();
                value = raw;
            }
            finally
            {
                renderer.ScopeManager.PopScope();
            }
        }
        else
        {
            var valueNode = setNode.Arguments.Last();
            value = renderer.Visit(valueNode);
        }

        foreach (var target in targets)
        {
            if (target is IdentifierNode identifier)
            {
                var currentScope = renderer.ScopeManager.CurrentScope();

                if (currentScope.ContainsKey("loop"))
                {
                    // In a loop: set only in current scope
                    currentScope[identifier.Name] = value;
                    // Mirror into TemplateContext as well to ensure lookups from different nested scopes
                    renderer.Context.Set(identifier.Name, value);
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
                    // Also mirror to TemplateContext so that variables set inside
                    // conditional blocks are visible to subsequent siblings.
                    renderer.Context.Set(identifier.Name, value);
                }
            }
            else if (target is AttributeNode attrNode)
            {
                var obj = renderer.Visit(attrNode.Object);
                if (obj == null) throw new InvalidOperationException($"Cannot set attribute '{attrNode.Attribute}' of null");

                SetMember(obj, attrNode.Attribute, value);
            }
        }


        return null;
    }

    private static HashSet<string> GetVariablesBeingSet(BlockNode node)
    {
        var setVariables = new HashSet<string>();

        void AnalyzeNode(ASTNode n)
        {
            if (n is BlockNode b)
            {
                if (b.Name == TemplateConstants.BlockNames.Set)
                {
                    foreach (var arg in b.Arguments.OfType<IdentifierNode>().Take(b.Arguments.Count - 1))
                    {
                        setVariables.Add(arg.Name);
                    }
                }

                if (b.Children != null)
                {
                    foreach (var child in b.Children)
                    {
                        AnalyzeNode(child);
                    }
                }
            }
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                AnalyzeNode(child);
            }
        }

        return setVariables;
    }

    private void SetMember(object obj, string memberName, object? value)
    {
        if (obj is IDictionary dict)
        {
            dict[memberName] = value;
            return;
        }

        // Try property
        var type = obj.GetType();
        var prop = type.GetProperty(memberName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (prop != null && prop.CanWrite)
        {
            // Simple assignment, might need type conversion if strict
            prop.SetValue(obj, value);
            return;
        }

        throw new InvalidOperationException($"Cannot set attribute '{memberName}' on object of type '{type.Name}'");
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