namespace Jinja2.NET.Interfaces;

public interface IVisitable
{
    object? Accept(INodeVisitor visitor);
    INodeRenderer GetRenderer();
}