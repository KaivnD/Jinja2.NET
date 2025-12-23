using System.Text;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

public class MacroBlockRenderer : INodeRenderer
{
    public object? Render(ASTNode node, IRenderer renderer)
    {
        if (node is not BlockNode block ||
            !block.Name.Equals(TemplateConstants.BlockNames.Macro, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected Macro BlockNode, got {node.GetType().Name}");
        }

        // Expect first argument to be macro name literal
        if (block.Arguments.Count == 0 || block.Arguments[0] is not LiteralNode nameNode)
        {
            throw new InvalidOperationException("Macro must have a name");
        }

        var macroName = nameNode.Value?.ToString() ?? string.Empty;
        var parameters = new List<string>();
        for (var i = 1; i < block.Arguments.Count; i++)
        {
            if (block.Arguments[i] is IdentifierNode id)
            {
                parameters.Add(id.Name);
            }
        }

        // Save MacroDefinition in the global template context
        var macroDef = new Jinja2.NET.Nodes.MacroDefinition(macroName, parameters, block.Children.ToList());
        renderer.Context.Set(macroName, macroDef);

        // Macro definitions do not output during render
        return null;
    }
}
