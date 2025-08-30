using Jinja2.NET.Interfaces;

namespace Jinja2.NET;

public class TemplateContext : IVariableContext
{
    private readonly List<Dictionary<string, object>> _scopes = new();
    private readonly Dictionary<string, object> _variables = new();

    public virtual object Get(string name)
    {
        // Check current scopes first (most recent to oldest)
        for (var i = _scopes.Count - 1; i >= 0; i--)
            if (_scopes[i].TryGetValue(name, out var value))
            {
                return value;
            }

        // Check global variables
        return _variables.TryGetValue(name, out var globalValue) ? globalValue : null;
    }

    public virtual void PopScope()
    {
        if (_scopes.Count > 0)
        {
            _scopes.RemoveAt(_scopes.Count - 1);
        }
    }

    public virtual void PushScope()
    {
        _scopes.Add(new Dictionary<string, object>());
    }

    public virtual void Set(string name, object value)
    {
        // Set in current scope if exists, otherwise global
        if (_scopes.Count > 0)
        {
            _scopes[_scopes.Count - 1][name] = value;
        }
        else
        {
            _variables[name] = value;
        }
    }

    public virtual void SetAll(Dictionary<string, object> variables)
    {
        foreach (var kvp in variables)
        {
            Set(kvp.Key, kvp.Value);
        }
    }

    // Add overload that LoggingTemplateContext expects
    public virtual void SetAll(object obj)
    {
        if (obj == null)
        {
            return;
        }

        switch (obj)
        {
            case Dictionary<string, object> dict:
                SetAll(dict);
                break;
            case TemplateContext tc:
                // Copy from another TemplateContext - would need implementation
                break;
            default:
                // Handle anonymous objects using reflection
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    Set(prop.Name, prop.GetValue(obj));
                }

                break;
        }
    }

    public virtual void SetVariableInGlobalScope(string name, object value)
    {
        _variables[name] = value;
    }
}