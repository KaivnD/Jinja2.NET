using System.Reflection;
using Jinja2.NET.Interfaces;

namespace Jinja2.NET.Nodes.Renderers;

public class MethodCallNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not MethodCallNode node)
        {
            throw new ArgumentException($"Expected MethodCallNode, got {nodeIn.GetType().Name}");
        }

        var obj = renderer.Visit(node.Object);
        if (obj == null)
        {
             // If object is null, we can't call a method on it.
             // In strict mode this might throw, but for now returning null or throwing is fine.
             // Following typical C# behavior:
             throw new NullReferenceException($"Object is null when calling method '{node.MethodName}'");
        }

        var args = node.Arguments.Select(arg => renderer.Visit(arg)).ToArray();
        // Ignoring Kwargs for now as C# reflection doesn't map 1:1 easily without more logic

        var methodName = node.MethodName;
        
        // Special handling for common string methods to match Jinja2 behavior
        if (obj is string str && methodName.Equals("split", StringComparison.OrdinalIgnoreCase))
        {
             if (args.Length == 1 && args[0] is string sep)
             {
                 return str.Split(new[] { sep }, StringSplitOptions.None);
             }
             if (args.Length == 0)
             {
                 // Default split by whitespace
                 return str.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
             }
        }
        // Additional Jinja-like string methods
        if (obj is string s)
        {
            if (methodName.Equals("strip", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                return s.Trim();
            }
            if (methodName.Equals("lstrip", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                return s.TrimStart();
            }
            if (methodName.Equals("rstrip", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                return s.TrimEnd();
            }
            if (methodName.Equals("title", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                try
                {
                    return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
                }
                catch
                {
                    return s;
                }
            }
            if (methodName.Equals("upper", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                return s.ToUpper();
            }
            if (methodName.Equals("lower", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                return s.ToLower();
            }
            if (methodName.Equals("capitalize", StringComparison.OrdinalIgnoreCase) && args.Length == 0)
            {
                if (s.Length == 0) return s;
                return char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : string.Empty);
            }
        }

        // General reflection
        var type = obj.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == args.Length)
            {
                try 
                {
                    // Attempt to convert args to parameter types
                    var convertedArgs = new object?[args.Length];
                    bool match = true;
                    for(int i=0; i<args.Length; i++)
                    {
                        if (args[i] == null)
                        {
                            if (parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null)
                            {
                                match = false; break;
                            }
                            convertedArgs[i] = null;
                        }
                        else
                        {
                            var argType = args[i]!.GetType();
                            if (parameters[i].ParameterType.IsAssignableFrom(argType))
                            {
                                 convertedArgs[i] = args[i];
                            }
                            else
                            {
                                try {
                                    convertedArgs[i] = Convert.ChangeType(args[i], parameters[i].ParameterType);
                                } catch {
                                    match = false; break;
                                }
                            }
                        }
                    }

                    if (match)
                    {
                        return method.Invoke(obj, convertedArgs);
                    }
                }
                catch
                {
                    // Ignore and try next overload
                }
            }
        }

        throw new InvalidOperationException($"Method '{methodName}' not found on type '{type.Name}' with provided arguments.");
    }
}
