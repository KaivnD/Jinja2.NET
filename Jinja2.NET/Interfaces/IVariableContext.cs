namespace Jinja2.NET.Interfaces;

public interface IVariableContext
{
    object Get(string name);
    void Set(string name, object value);
    void SetAll(Dictionary<string, object> variables);
}