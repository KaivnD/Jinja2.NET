using Jinja2.NET.Interfaces;

namespace Jinja2.NET;

public class ScopeManager : IScopeManager
{
    private readonly Stack<Dictionary<string, object>> _scopes = new();

    public int ScopeDepth => _scopes.Count;

    public ScopeManager()
    {
        _scopes.Push(new Dictionary<string, object>());
    }

    public Dictionary<string, object> CurrentScope()
    {
        return _scopes.Peek();
    }

    public Dictionary<string, object> ParentScope()
    {
        return _scopes.Count > 1 ? _scopes.Skip(1).First() : new Dictionary<string, object>();
    }

    public void PopScope()
    {
        _scopes.Pop();
    }

    public void PropagateVariablesToParent(IVariableContext context, string loopVarName, HashSet<string> setVariables)
    {
        var currentScope = CurrentScope();
        var parentScope = ParentScope();
        foreach (var variable in setVariables)
        {
            if (variable != loopVarName && currentScope.ContainsKey(variable))
            {
                parentScope[variable] = currentScope[variable];
            }
        }
    }

    public void PushScope()
    {
        _scopes.Push(new Dictionary<string, object>());
    }
}