using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Parsers;

public class TagParserRegistry : ITagParserRegistry
{
    private readonly Dictionary<string, ITagParser> _parsers = new();

    public void ClearParsers()
    {
        _parsers.Clear();
    }

    public ITagParser? GetParser(string tagName)
    {
        _parsers.TryGetValue(tagName.ToLower(), out var parser);
        return parser;
    }

    public IEnumerable<string> GetRegisteredTagNames()
    {
        return _parsers.Keys;
    }

    public bool HasParser(string tagName)
    {
        return _parsers.ContainsKey(tagName.ToLower());
    }

    public void RegisterParser(string tagName, ITagParser parser)
    {
        _parsers[tagName.ToLower()] = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    public void UnregisterParser(string tagName)
    {
        _parsers.Remove(tagName.ToLower());
    }
}