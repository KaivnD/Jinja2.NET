namespace Jinja2.NET.Interfaces;

public interface ITemplateContext
{
    object Get(string name);
    void PopScope();
    void PushScope();
    void Set(string name, object value);
    void SetAll(object obj);
}