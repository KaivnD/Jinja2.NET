namespace Jinja2.NET;

public class TemplateParsingException : Exception
{
    public string Source { get; set; }
    public IReadOnlyList<Token> Tokens { get; set; }
    public EParsingStage Stage { get; set; }

    public TemplateParsingException(string message) : base(message) { }
    public TemplateParsingException(string message, Exception innerException) : base(message, innerException) { }
}