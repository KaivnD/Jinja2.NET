using System.Diagnostics;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Helpers;

public sealed class XUnitTraceListener : TraceListener
{
    private readonly string _name;
    private readonly ITestOutputHelper _output;

    public override string Name => _name;

    public XUnitTraceListener(ITestOutputHelper output, string name = "XUnitTrace")
    {
        _output = output;
        _name = name;
    }

    public override void Write(string? message)
    {
        /* ignore partial fragments */
    }

    public override void WriteLine(string? message)
    {
        try
        {
            if (message != null)
            {
                _output.WriteLine(message);
            }
        }
        catch (InvalidOperationException)
        {
            // Swallow: no active test context, ignore trace
        }
    }
}