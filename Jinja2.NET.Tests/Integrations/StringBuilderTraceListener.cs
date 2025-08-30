using System.Diagnostics;
using System.Text;

namespace Jinja2.NET.Tests.Integrations
{
    public class StringBuilderTraceListener : TraceListener
  {
    private readonly StringBuilder _sb = new();

    public override void Write(string message) => _sb.Append(message);
    public override void WriteLine(string message) => _sb.AppendLine(message);
    public override string ToString() => _sb.ToString();
  }
}