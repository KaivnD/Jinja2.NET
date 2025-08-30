using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class LoopProcessorFactory : ILoopProcessorFactory
{
    private readonly List<(Func<BlockNode, IScopeManager, bool> Condition, Func<ILoopProcessor> Creator)>
        _processorRegistry;

    public LoopProcessorFactory()
    {
        _processorRegistry = new List<(Func<BlockNode, IScopeManager, bool>, Func<ILoopProcessor>)>
        {
            ((node, scopeManager) => IsRecursiveLoop(node), () => new RecursiveLoopProcessor()),
            // Detect nested loops by checking the current scope for the loop object
            ((node, scopeManager) => scopeManager.CurrentScope().ContainsKey("loop"), () => new NestedLoopProcessor()),
            ((node, scopeManager) => true, () => new StandardLoopProcessor())
        };
    }

    public ILoopProcessor CreateProcessor(BlockNode node, IScopeManager scopeManager)
    {
        foreach (var (condition, creator) in _processorRegistry)
        {
            if (condition(node, scopeManager))
            {
                return creator();
            }
        }

        return new StandardLoopProcessor();
    }

    public void RegisterProcessor(Func<BlockNode, IScopeManager, bool> condition, Func<ILoopProcessor> processorCreator)
    {
        _processorRegistry.Insert(0, (condition, processorCreator));
    }

    private static bool IsRecursiveLoop(BlockNode node)
    {
        return node.Arguments?.OfType<IdentifierNode>().Any(n => n.Name == "recursive") ?? false;
    }
}