using System.Diagnostics;

namespace Jinja2.NET;

public class VariableContext
{
    private readonly Dictionary<string, object> _variables;

    public VariableContext()
    {
        _variables = new Dictionary<string, object>();
    }

    public virtual object GetValue(string name)
    {
        var found = _variables.TryGetValue(name, out var value);
        Trace.WriteLine($"TRACE: GetValue: {name} = {(found ? value : "null")} (from VariableContext)");
        return found ? value : null;
    }

    public virtual void SetAll(Dictionary<string, object> values)
    {
        foreach (var kvp in values)
        {
            SetValue(kvp.Key, kvp.Value);
        }
    }

    public virtual void SetValue(string name, object value)
    {
        Trace.WriteLine($"TRACE: SetValue: {name} = {value} (in VariableContext)");
        _variables[name] = value;
    }
}