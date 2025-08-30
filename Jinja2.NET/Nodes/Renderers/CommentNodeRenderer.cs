using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class CommentNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        // Do nothing for comments
        return null;
    }
}