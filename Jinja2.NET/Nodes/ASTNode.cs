using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes;

public abstract class ASTNode
{
    public abstract object? Accept(INodeVisitor visitor);
}