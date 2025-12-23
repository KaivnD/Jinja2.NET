using Jinja2.NET.Nodes;

namespace Jinja2.NET.Nodes;

public class MacroDefinition
{
    public string Name { get; }
    public List<string> Parameters { get; }
    public List<ASTNode> Body { get; }

    public MacroDefinition(string name, List<string> parameters, List<ASTNode> body)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Parameters = parameters ?? new List<string>();
        Body = body ?? new List<ASTNode>();
    }
}
