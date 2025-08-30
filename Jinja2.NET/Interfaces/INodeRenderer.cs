using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface INodeRenderer
{
    object? Render(ASTNode node, IRenderer renderer);
}