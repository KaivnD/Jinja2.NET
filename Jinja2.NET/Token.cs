namespace Jinja2.NET;

public class Token
{
    public int Column { get; }
    public int Line { get; }
    public bool TrimLeft { get; }
    public bool TrimRight { get; }
    public ETokenType Type { get; }
    public string Value { get; }

    public Token(ETokenType type, string value, int line, int column, bool trimLeft = false, bool trimRight = false)
    {
        Type = type;
        Value = value;
        Line = line;
        Column = column;
        TrimLeft = trimLeft;
        TrimRight = trimRight;
    }
}