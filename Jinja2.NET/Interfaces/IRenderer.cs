using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface IRenderer
{
    IVariableContext Context { get; }
    IReadOnlyDictionary<string, Func<object, object[], object>> CustomFilters { get; }
    IScopeManager ScopeManager { get; }
    object Visit(ASTNode node);
    string Render(TemplateNode template); 
}