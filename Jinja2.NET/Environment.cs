namespace Jinja2.NET;

public class Environment
{
    private readonly Dictionary<string, Template> _templateCache = new();
    private readonly string? _templateDirectory;

    public Environment(string? templateDirectory = null)
    {
        _templateDirectory = templateDirectory;
    }

    public void ClearCache()
    {
        _templateCache.Clear();
    }

    public Template FromString(string source)
    {
        return new Template(source);
    }

    public Template GetTemplate(string name)
    {
        if (_templateCache.TryGetValue(name, out var cached))
        {
            return cached;
        }

        string source;
        if (_templateDirectory != null)
        {
            var path = Path.Combine(_templateDirectory, name);
            source = File.ReadAllText(path);
        }
        else
        {
            throw new ArgumentException("No template directory specified and template not in cache");
        }

        var template = new Template(source);
        _templateCache[name] = template;
        return template;
    }
}