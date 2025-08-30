using System.Reflection;

namespace Jinja2.NET;

public static class AttributeResolver
{
    public static object? GetAttribute(object obj, string attribute)
    {
        if (obj == null)
        {
            return null;
        }

        if (obj is Dictionary<string, object> dict && dict.TryGetValue(attribute, out var value))
        {
            return value;
        }

        var type = obj.GetType();
        var property =
            type.GetProperty(attribute, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property != null)
        {
            return property.GetValue(obj);
        }

        var field = type.GetField(attribute, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (field != null)
        {
            return field.GetValue(obj);
        }

        return null;
    }
}