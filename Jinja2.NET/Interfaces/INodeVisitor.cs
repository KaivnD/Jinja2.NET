using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface INodeVisitor
{
    object? Visit(ASTNode node);
}