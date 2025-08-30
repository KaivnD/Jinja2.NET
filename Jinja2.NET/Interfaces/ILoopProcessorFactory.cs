using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface ILoopProcessorFactory
{
    ILoopProcessor CreateProcessor(BlockNode node, IScopeManager scopeManager);
}