using System.Collections;
using System.Text;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Nodes.Renderers;

public class FunctionCallNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        ValidateNodeType(nodeIn);
        var node = (FunctionCallNode)nodeIn;

        var fname = node.FunctionName.ToLowerInvariant();
        object? result;
        switch (fname)
        {
            case "loop":
                result = HandleRecursiveLoop(node, renderer);
                break;
            case "range":
                result = HandleRange(node, renderer);
                break;
            case "namespace":
                result = HandleNamespace(node, renderer);
                break;
            case "dict":
                result = HandleDict(node, renderer);
                break;
            case "raise_exception":
                result = HandleRaiseException(node, renderer);
                break;
            default:
                result = HandleMacroInvocation(node, renderer);
                break;
        }

        return result;
    }

    private object? HandleRaiseException(FunctionCallNode node, IRenderer renderer)
    {
        if (node.Arguments.Count == 0)
        {
            throw new InvalidOperationException("raise_exception requires a message argument");
        }

        var msgObj = renderer.Visit(node.Arguments[0]);
        var message = msgObj?.ToString() ?? string.Empty;
        throw new InvalidOperationException(message);
    }

    private object? HandleNamespace(FunctionCallNode node, IRenderer renderer)
    {
        var ns = new Dictionary<string, object?>();
        
        if (node.Arguments.Count > 0)
        {
             var arg = renderer.Visit(node.Arguments[0]);
             if (arg is IDictionary dict)
             {
                 foreach(DictionaryEntry de in dict)
                 {
                     ns[de.Key.ToString()!] = de.Value;
                 }
             }
        }

        foreach (var kvp in node.Kwargs)
        {
            ns[kvp.Key] = renderer.Visit(kvp.Value);
        }

        return ns;
    }

    private object? HandleDict(FunctionCallNode node, IRenderer renderer)
    {
        var result = new Dictionary<string, object?>();

        // If first positional arg is a dictionary-like object, copy its entries
        if (node.Arguments.Count > 0)
        {
            var argVal = renderer.Visit(node.Arguments[0]);
            if (argVal is System.Collections.IDictionary idict)
            {
                foreach (System.Collections.DictionaryEntry de in idict)
                {
                    result[de.Key?.ToString() ?? string.Empty] = de.Value;
                }
            }
        }

        // Merge kwargs (they override entries from the first arg)
        foreach (var kvp in node.Kwargs)
        {
            result[kvp.Key] = renderer.Visit(kvp.Value);
        }

        return result;
    }

    private object? HandleRange(FunctionCallNode node, IRenderer renderer)
    {
        var args = node.Arguments.Select(arg => Convert.ToInt32(renderer.Visit(arg))).ToList();
        
        int start = 0;
        int stop = 0;
        int step = 1;

        if (args.Count == 1)
        {
            stop = args[0];
        }
        else if (args.Count == 2)
        {
            start = args[0];
            stop = args[1];
        }
        else if (args.Count == 3)
        {
            start = args[0];
            stop = args[1];
            step = args[2];
        }

        if (step == 0) throw new ArgumentException("range() step argument must not be zero");

        var result = new List<int>();
        if (step > 0)
        {
            for (var i = start; i < stop; i += step) result.Add(i);
        }
        else
        {
            for (var i = start; i > stop; i += step) result.Add(i);
        }
        return result;
    }

    private static int CalculateInPosition(BlockNode forTemplate, bool hasRecursive)
    {
        return hasRecursive ? forTemplate.Arguments.Count - 3 : forTemplate.Arguments.Count - 2;
    }

    private static IEnumerable<object> ConvertToEnumerable(object value)
    {
        return value switch
        {
            null => Enumerable.Empty<object>(),
            string str => str.Select(c => (object)c),
            IEnumerable<object> enumerable => enumerable,
            IEnumerable enumerable => enumerable.Cast<object>(),
            _ => new[] { value }
        };
    }

    private static List<string> ExtractLoopVariableNames(BlockNode forTemplate)
    {
        var hasRecursive = HasRecursiveKeyword(forTemplate);
        var inPosition = CalculateInPosition(forTemplate, hasRecursive);

        return forTemplate.Arguments
            .Take(inPosition)
            .OfType<IdentifierNode>()
            .Select(n => n.Name)
            .ToList();
    }

    private static BlockNode FindRecursiveTemplate(IScopeManager scopeManager)
    {
        return FindTemplateInCurrentScope(scopeManager) ?? FindTemplateInParentScope(scopeManager);
    }

    private static BlockNode FindTemplateInCurrentScope(IScopeManager scopeManager)
    {
        var currentScope = scopeManager.CurrentScope();
        return TryGetTemplateFromScope(currentScope);
    }

    private static BlockNode FindTemplateInParentScope(IScopeManager scopeManager)
    {
        var parentScope = scopeManager.ParentScope();
        return TryGetTemplateFromScope(parentScope);
    }

    private List<object> GetIterableItems(FunctionCallNode node, IRenderer renderer)
    {
        var newIterable = renderer.Visit(node.Arguments[0]);

        if (newIterable == null)
        {
            return new List<object>();
        }

        var items = ConvertToEnumerable(newIterable);
        return items.ToList();
    }

    private static BlockNode GetRecursiveTemplate(IRenderer renderer)
    {
        var forTemplate = FindRecursiveTemplate(renderer.ScopeManager);
        if (forTemplate == null)
        {
            throw new InvalidOperationException(
                "Recursive loop function can only be called within a recursive for loop");
        }

        return forTemplate;
    }

    private object? HandleRecursiveLoop(FunctionCallNode node, IRenderer renderer)
    {
        ValidateRecursiveLoopArguments(node);

        var forTemplate = GetRecursiveTemplate(renderer);
        var items = GetIterableItems(node, renderer);

        if (items.Count == 0)
        {
            return "";
        }

        return RenderRecursiveItems(items, forTemplate, renderer);
    }

    private static bool HasRecursiveKeyword(BlockNode forTemplate)
    {
        return forTemplate.Arguments.LastOrDefault() is IdentifierNode { Name: "recursive" };
    }

    private static bool IsElseBlock(ASTNode child)
    {
        return child is BlockNode { Name: "else" };
    }

    private static void PreserveRecursiveTemplate(BlockNode forTemplate, IDictionary<string, object> scope)
    {
        scope["__recursive_template__"] = forTemplate;
    }

    private static void RenderLoopBody(BlockNode forTemplate, IRenderer renderer, StringBuilder result)
    {
        foreach (var child in forTemplate.Children)
        {
            if (IsElseBlock(child))
            {
                continue;
            }

            var childResult = renderer.Visit(child);
            if (childResult != null)
            {
                result.Append(childResult);
            }
        }
    }

    private string RenderRecursiveItems(List<object> items, BlockNode forTemplate, IRenderer renderer)
    {
        var result = new StringBuilder();
        var loopVarNames = ExtractLoopVariableNames(forTemplate);

        renderer.ScopeManager.PushScope();
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                SetupLoopContext(items[i], i, items.Count, loopVarNames, forTemplate, renderer);
                RenderLoopBody(forTemplate, renderer, result);
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
        }

        return result.ToString();
    }

    private static void SetLoopMetadata(int index, int totalCount, IDictionary<string, object> scope)
    {
        scope["loop"] = new
        {
            index0 = index,
            index = index + 1,
            first = index == 0,
            last = index == totalCount - 1,
            length = totalCount
        };
    }

    private static void SetLoopVariables(object item, List<string> loopVarNames, IDictionary<string, object> scope)
    {
        if (ShouldUnpackItem(item, loopVarNames))
        {
            UnpackMultipleVariables(item, loopVarNames, scope);
        }
        else
        {
            SetSingleVariable(item, loopVarNames, scope);
        }
    }

    private static void SetSingleVariable(object item, List<string> loopVarNames, IDictionary<string, object> scope)
    {
        if (loopVarNames.Count > 0)
        {
            scope[loopVarNames[0]] = item;
        }
    }

    private static void SetupLoopContext(object item, int index, int totalCount,
        List<string> loopVarNames, BlockNode forTemplate, IRenderer renderer)
    {
        var scope = renderer.ScopeManager.CurrentScope();

        SetLoopVariables(item, loopVarNames, scope);
        SetLoopMetadata(index, totalCount, scope);
        PreserveRecursiveTemplate(forTemplate, scope);
    }

    private static bool ShouldUnpackItem(object item, List<string> loopVarNames)
    {
        return item is IEnumerable and not string && loopVarNames.Count > 1;
    }

    private static BlockNode? TryGetTemplateFromScope(IDictionary<string, object> scope)
    {
        if (scope.TryGetValue("__recursive_template__", out var templateObj) &&
            templateObj is BlockNode template)
        {
            return template;
        }

        return null;
    }

    private static void UnpackMultipleVariables(object item, List<string> loopVarNames,
        IDictionary<string, object> scope)
    {
        var values = ((IEnumerable)item).Cast<object>().ToList();
        for (var j = 0; j < Math.Min(loopVarNames.Count, values.Count); j++) scope[loopVarNames[j]] = values[j];
    }

    private static void ValidateNodeType(ASTNode nodeIn)
    {
        if (nodeIn is not FunctionCallNode)
        {
            throw new ArgumentException($"Expected FunctionCallNode, got {nodeIn.GetType().Name}");
        }
    }

    private static void ValidateRecursiveLoopArguments(FunctionCallNode node)
    {
        if (node.Arguments.Count == 0)
        {
            throw new InvalidOperationException("Recursive loop function requires at least one argument");
        }
    }

    private object? HandleMacroInvocation(FunctionCallNode node, IRenderer renderer)
    {
        // Look up macro definition from context
        var macroObj = renderer.Context.Get(node.FunctionName);
        if (macroObj is not MacroDefinition macroDef)
        {
            // If the template declares a macro with this name (but it may be defined later),
            // treat as call-before-definition and throw NotSupportedException to match tests.
            var known = renderer.Context.Get("__defined_macros__");
            if (known is IEnumerable<string> names && names.Contains(node.FunctionName, StringComparer.OrdinalIgnoreCase))
            {
                throw new NotSupportedException($"Function '{node.FunctionName}' is not supported");
            }

            throw new InvalidOperationException($"Function '{node.FunctionName}' is not supported");
        }

        // Map arguments to parameters
        var sb = new StringBuilder();
        renderer.ScopeManager.PushScope();
        try
        {
            var scope = renderer.ScopeManager.CurrentScope();
            for (var i = 0; i < macroDef.Parameters.Count; i++)
            {
                var param = macroDef.Parameters[i];
                object? value = null;
                if (i < node.Arguments.Count)
                {
                    value = renderer.Visit(node.Arguments[i]);
                }

                scope[param] = value;
            }

            // Render macro body
            // Apply whitespace trimming rules for the macro body before rendering
            WhitespaceTrimmer.ApplyWhitespaceTrimming(macroDef.Body);
            foreach (var child in macroDef.Body)
            {
                var childResult = renderer.Visit(child);
                if (childResult != null)
                {
                    sb.Append(childResult);
                }
            }
        }
        finally
        {
            renderer.ScopeManager.PopScope();
        }

        return sb.ToString();
    }
}