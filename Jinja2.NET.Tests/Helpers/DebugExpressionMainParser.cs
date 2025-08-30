using Xunit.Abstractions;

namespace Jinja2.NET.Tests.Helpers;

public class DebugExpressionMainParser : MainParser
{
    public DebugExpressionMainParser(ITestOutputHelper output, LexerConfig config = null) : base(config)
    {
        ReplaceExpressionParser(new DebugExpressionParser(output));
    }
}