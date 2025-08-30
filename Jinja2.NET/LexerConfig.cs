namespace Jinja2.NET;

public class LexerConfig
{
    public Dictionary<string, string[]> EndDelimiters { get; set; } = new()
    {
        ["{{"] = new[] { "}}", "-}}" },
        ["{%-"] = new[] { "%}", "-%}" },
        ["{%"] = new[] { "%}", "-%}" },
        ["{#"] = new[] { "#}", "-#}" },
        ["{#-"] = new[] { "#}", "-#}" },
        ["{{-"] = new[] { "}}", "-}}" }
    };

    /// <summary>
    ///     Gets or sets a value indicating whether leading whitespace should be stripped from the start of blocks.
    /// </summary>
    public bool LstripBlocks { get; set; } = false;

    public string[] StartDelimiters { get; set; } = new[] { "{{-", "{%-", "{#-", "{{", "{%", "{#" };

    public string TokenPattern { get; set; } =
        @"(\{\{|\}\}|\{\%|\%\}|\{\#|\#\}|([a-zA-Z_][a-zA-Z0-9_]*)|(\d+(?:\.\d+)?)|(""[^""]*""|'[^']*')|[+\-*/=<>!]=?|[|.(),\[\]:=]|\s+)";
}