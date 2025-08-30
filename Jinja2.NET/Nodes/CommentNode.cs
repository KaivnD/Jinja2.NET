using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes.Renderers;

namespace Jinja2.NET.Nodes;

public class CommentNode : ASTNode, IVisitable
{
    public string Content { get; }
    public bool TrimLeft { get; set; }
    public bool TrimRight { get; set; }

    public CommentNode(string content, bool trimLeft = false, bool trimRight = false)
    {
        Content = content;
        TrimLeft = trimLeft;
        TrimRight = trimRight;
    }

    public override object Accept(INodeVisitor visitor)
    {
        return null;
        // Comments are not rendered
    }

    public INodeRenderer GetRenderer()
    {
        return new CommentNodeRenderer();
    }
}