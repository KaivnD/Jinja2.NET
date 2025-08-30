using System.Diagnostics;
using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Helpers;

public static class TestTraceSetup
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    public static void EnsureListener(ITestOutputHelper output)
    {
        lock (_lock)
        {
            if (!_initialized)
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(new XUnitTraceListener(output));
                _initialized = true;
            }
        }
    }
}