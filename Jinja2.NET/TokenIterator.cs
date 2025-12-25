using Jinja2.NET.Models;

namespace Jinja2.NET;

public class TokenIterator
{
    private readonly List<Token> _tokens;
    private int _current;
    private Token _lastConsumedMarker;

    public SourceLocation CurrentLocation
    {
        get
        {
            if (_tokens.Count == 0)
            {
                return new SourceLocation(1, 1, 0);
            }

            if (_current < _tokens.Count)
            {
                return new SourceLocation(_tokens[_current].Line, _tokens[_current].Column, _current);
            }

            // At or past the end: use the last token's location
            var last = _tokens[_tokens.Count - 1];
            return new SourceLocation(last.Line, last.Column, _current);
        }
    }

    public TokenIterator(List<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _current = 0;
    }

    public Token Consume(ETokenType expectedType)
    {
        if (IsAtEnd() || Peek().Type != expectedType)
        {
            throw new InvalidOperationException(
                $"Expected {expectedType} at line {CurrentLocation.Line}, column {CurrentLocation.Column}");
        }

        var token = _tokens[_current++];
        if (token.Type is ETokenType.VariableStart or ETokenType.VariableEnd or ETokenType.BlockStart
            or ETokenType.BlockEnd
            or ETokenType.CommentStart or ETokenType.CommentEnd)
        {
            _lastConsumedMarker = token;
        }

        return token;
    }

    public bool IsAtEnd()
    {
        return _current >= _tokens.Count || Peek().Type == ETokenType.EOF;
    }

    public static bool IsSpaceOrTabOnly(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        foreach (var c in value)
        {
            if (c != ' ' && c != '\t')
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsAllWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    public void SkipAllWhitespace()
    {
        while (!IsAtEnd() && Peek().Type == ETokenType.Text && IsAllWhitespace(Peek().Value))
            Consume(ETokenType.Text);
    }

    public bool Match(ETokenType type, string value)
    {
        if (!IsAtEnd() && Peek().Type == type && Peek().Value.Equals(value, StringComparison.OrdinalIgnoreCase))
        {
            Consume(type);
            return true;
        }

        return false;
    }

    public Token Peek(int lookahead = 0)
    {
        var index = _current + lookahead;
        if (index < 0)
        {
            return _tokens[0];
        }

        if (index >= _tokens.Count)
        {
            return _tokens[_tokens.Count - 1];
        }

        return _tokens[index];
    }

    public void SkipWhitespace()
    {
        while (!IsAtEnd() && Peek().Type == ETokenType.Text && IsSpaceOrTabOnly(Peek().Value))
            Consume(ETokenType.Text);
    }

    public bool WasLastConsumedMarkerTrimEnd()
    {
        return _lastConsumedMarker != null && (_lastConsumedMarker.TrimRight || _lastConsumedMarker.TrimLeft);
    }
}