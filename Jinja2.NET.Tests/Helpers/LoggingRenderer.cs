using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Helpers;

public class LoggingRenderer
{
    private readonly Renderer _inner;
    private readonly ITestOutputHelper _output;

    public IVariableContext Context => _inner.Context; 
    //public StringBuilder Output => _inner.Output;


    public LoggingRenderer(Renderer inner, ITestOutputHelper output)
    {
        _inner = inner;
        _output = output;
    }

    public void Render(TemplateNode node)
    {
        _output.WriteLine($"Render: Node={node.GetType().Name}");
        var result = _inner.Render(node); // Capture the returned string
        _output.WriteLine($"Render Complete: Output={result}"); // Use the returned result
    }

    public object Visit(ASTNode node)
    {
        _output.WriteLine($"Visit: Node={node.GetType().Name}, Name={(node is IdentifierNode id ? id.Name : "N/A")}");
        var result = _inner.Visit(node);
        _output.WriteLine($"Visit Result: Node={node.GetType().Name}, Result={result}");
        return result;
    }
}