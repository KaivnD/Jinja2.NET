namespace Jinja2.NET.Interfaces;

public interface IScopeManager
{
    Dictionary<string, object> CurrentScope();
    Dictionary<string, object> ParentScope();
    void PopScope();
    void PropagateVariablesToParent(IVariableContext context, string loopVarName, HashSet<string> setVariables);
    void PushScope();
}