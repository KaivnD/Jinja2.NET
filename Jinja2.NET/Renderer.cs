using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;
using Jinja2.NET.Nodes.Renderers;
using Jinja2.NET.Nodes.Renderers.BlockNodeSupport;

namespace Jinja2.NET;

public class Renderer : IRenderer
{
    private readonly StringBuilder _output = new();
    private readonly Dictionary<Type, INodeRenderer> _renderers = new();
    public IVariableContext Context { get; }
    public IReadOnlyDictionary<string, Func<object, object[], object>> CustomFilters { get; } // ✅ Add this
    public IScopeManager ScopeManager { get; }

    public Renderer(IVariableContext context, IScopeManager? scopeManager = null)
        : this(context, scopeManager, new Dictionary<string, Func<object, object[], object>>())
    {
    }

    public Renderer(IVariableContext context,
        Dictionary<string, Func<object, object[], object>> customFilters) // ✅ Add this constructor
        : this(context, null, customFilters)
    {
    }

    public Renderer(IVariableContext context, IScopeManager? scopeManager,
        Dictionary<string, Func<object, object[], object>> customFilters)
    {
        Context = context;
        ScopeManager = scopeManager ?? new ScopeManager();
        CustomFilters = customFilters?.AsReadOnly() ??
                        new Dictionary<string, Func<object, object[], object>>().AsReadOnly();

        // Register all required node renderers
        _renderers[typeof(BlockNode)] = new BlockNodeRenderer();
        _renderers[typeof(IdentifierNode)] = new IdentifierNodeRenderer();
        _renderers[typeof(TextNode)] = new TextNodeRenderer();
        _renderers[typeof(VariableNode)] = new VariableNodeRenderer();
        _renderers[typeof(FilterNode)] = new FilterNodeRenderer();
        _renderers[typeof(RawNode)] = new RawNodeRenderer();
        _renderers[typeof(LiteralNode)] = new LiteralNodeRenderer();
        _renderers[typeof(ListLiteralNode)] = new ListLiteralNodeRenderer();
        _renderers[typeof(UnaryExpressionNode)] = new UnaryExpressionNodeRenderer();
        _renderers[typeof(BinaryExpressionNode)] = new BinaryExpressionNodeRenderer();
        _renderers[typeof(IndexNode)] = new IndexNodeRenderer();
        _renderers[typeof(AttributeNode)] = new AttributeNodeRenderer();
        _renderers[typeof(CommentNode)] = new CommentNodeRenderer();
        _renderers[typeof(FunctionCallNode)] = new FunctionCallNodeRenderer();
        _renderers[typeof(MethodCallNode)] = new MethodCallNodeRenderer();
    }

    public string Render(TemplateNode template)
    {
        _output.Clear();

        WhitespaceTrimmer.ApplyWhitespaceTrimming(template.Children);

        foreach (var node in template.Children)
        {
            var result = Visit(node);
            if (result != null)
            {
                _output.Append(result);
            }
        }

        return _output.ToString();
    }

    public object? Visit(ASTNode node)
    {
        if (_renderers.TryGetValue(node.GetType(), out var renderer))
        {
            if (renderer != null)
            {
                return renderer.Render(node, this);
            }
        }

        throw new NotSupportedException($"No renderer for node type {node.GetType().Name}");
    }
}