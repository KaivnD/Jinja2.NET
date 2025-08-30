using Jinja2.NET.Nodes;

namespace Jinja2.NET.Interfaces;

public interface ILoopProcessor
{
    string Process(IRenderer renderer, BlockNode node, List<string> loopVarNames, List<object> items,
        HashSet<string> setVariables); // Change void to string
}