using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Helpers;

public class LoggingTemplateContext : TemplateContext
{
    private readonly ITestOutputHelper _output;

    public LoggingTemplateContext(ITestOutputHelper output)
    {
        _output = output;
    }

    public override object? Get(string name)
    {
        var value = base.Get(name);
        _output.WriteLine($"Get: {name} = {(value == null ? "null" : value.ToString())}");
        return value;
    }

    public override void PopScope()
    {
        _output.WriteLine("PopScope");
        base.PopScope();
    }

    public override void PushScope()
    {
        _output.WriteLine("PushScope");
        base.PushScope();
    }

    public override void Set(string name, object? value)
    {
        if (value != null && name == "i")
        {
            _output.WriteLine($"Debug: Setting loop variable i = {value}");
        }

        _output.WriteLine($"Set: {name} = {(value == null ? "null" : value.ToString())}");
        if (value != null)
        {
            base.Set(name, value);
        }
    }

    public override void SetAll(object? obj)
    {
        _output.WriteLine($"SetAll: {(obj == null ? "null" : obj.ToString())}");
        base.SetAll(obj);
    }

    public override void SetVariableInGlobalScope(string name, object? value)
    {
        _output.WriteLine($"SetInRootScope: {name} = {(value == null ? "null" : value.ToString())}");
        base.SetVariableInGlobalScope(name, value);
    }
}