namespace Jinja2.NET;

public static class LexerExtensions
{
    public static IReadOnlyList<Token> GetPartialTokens(this Lexer lexer)
    {
        // This would need to be implemented in your Lexer class
        // Return whatever tokens were successfully parsed before the error
        return Array.Empty<Token>();
    }
}