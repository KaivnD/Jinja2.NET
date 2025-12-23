using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Jinja2.NET;

public static class BuiltinFilters
{
    public const string UpperFilter = "upper";
    public const string LowerFilter = "lower";
    public const string LengthFilter = "length";
    public const string DefaultFilter = "default";
    public const string CapitalizeFilter = "capitalize";
    public const string TitleFilter = "title";
    public const string TrimFilter = "trim";
    public const string ReplaceFilter = "replace";
    public const string JoinFilter = "join";
    public const string ReverseFilter = "reverse";
    public const string SortFilter = "sort";
    public const string FirstFilter = "first";
    public const string LastFilter = "last";
    public const string ToJsonFilter = "tojson";

    // Other magic values
    private const string EmptyString = "";
    static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 核心：不转义非ASCII字符
        WriteIndented = false, // 格式化输出
        AllowTrailingCommas = true // 可选：允许末尾逗号，增强兼容性
    };
    private static readonly Dictionary<string, Func<object?, object[], object>> _filters =
        new()
        {
            [UpperFilter] = (value, args) => value?.ToString()?.ToUpper(),
            [LowerFilter] = (value, args) => value?.ToString()?.ToLower(),
            [LengthFilter] = (value, args) => GetLength(value),
            [DefaultFilter] = (value, args) => value ?? (args.Length > 0 ? args[0] : EmptyString),
            [CapitalizeFilter] = (value, args) => Capitalize(value?.ToString()),
            [TitleFilter] = (value, args) => ToTitleCase(value?.ToString()),
            [TrimFilter] = (value, args) => value?.ToString()?.Trim(),
            [ReplaceFilter] = (value, args) => DoReplaceFilter(value?.ToString(), args),
            [JoinFilter] = (value, args) => DoJoinFilter(value, args),
            [ReverseFilter] = (value, args) => DoReverseFilter(value),
            [SortFilter] = (value, args) => DoSortFilter(value),
            [FirstFilter] = (value, args) => DoFirstFilter(value),
            [LastFilter] = (value, args) => DoLastFilter(value),
            ["selectattr"] = (value, args) => DoSelectAttr(value, args),
            ["map"] = (value, args) => DoMapFilter(value, args),
            ["list"] = (value, args) => DoListFilter(value),
            [ToJsonFilter] = (value, args) => JsonSerializer.Serialize(value, jsonOptions)
        };

    public static object ApplyFilter(string filterName, object? value, object[] arguments)
    {
        if (_filters.TryGetValue(filterName, out var filter))
        {
            return filter(value, arguments);
        }

        throw new ArgumentException($"Unknown filter: {filterName}");
    }

    public static bool HasFilter(string filterName)
    {
        return _filters.ContainsKey(filterName);
    }

    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return char.ToUpper(value[0]) + value.Substring(1).ToLower();
    }

    private static object DoFirstFilter(object value)
    {
        if (value is IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>().FirstOrDefault();
        }

        return value;
    }

    private static object DoListFilter(object value)
    {
        if (value == null) return new List<object>();
        if (value is string s) return s.Select(c => (object)c).ToList();
        if (value is IEnumerable e) return e.Cast<object>().ToList();
        return new List<object> { value };
    }

    private static object DoMapFilter(object value, object[] args)
    {
        if (value == null) return new List<object>();

        var items = value is string str ? str.Select(c => (object)c) : (value is IEnumerable ie ? ie.Cast<object>() : new[] { value }.AsEnumerable());

        // Determine attribute name from args or kwargs-like dictionary
        string? attrName = null;
        if (args != null && args.Length > 0)
        {
            // If single arg is a dictionary-like object produced by kwargs parsing
            if (args.Length == 1 && args[0] is System.Collections.IDictionary dict)
            {
                if (dict.Contains("attribute")) attrName = dict["attribute"]?.ToString();
                else if (dict.Contains("0")) attrName = dict["0"]?.ToString();
            }
            else
            {
                attrName = args[0]?.ToString();
            }
        }

        if (string.IsNullOrEmpty(attrName))
        {
            // If no attribute provided, just return list of items
            return items.ToList();
        }

        var result = new List<object>();
        foreach (var it in items)
        {
            if (it == null)
            {
                result.Add(null);
                continue;
            }

            // IDictionary support
            if (it is System.Collections.IDictionary idict)
            {
                if (idict.Contains(attrName)) result.Add(idict[attrName]);
                else result.Add(null);
                continue;
            }

            // Try reflection for property or field
            var t = it.GetType();
            var prop = t.GetProperty(attrName);
            if (prop != null)
            {
                result.Add(prop.GetValue(it));
                continue;
            }

            var field = t.GetField(attrName);
            if (field != null)
            {
                result.Add(field.GetValue(it));
                continue;
            }

            // Fallback: try dictionary-like access via indexer
            try
            {
                var indexer = t.GetProperty("Item");
                if (indexer != null)
                {
                    result.Add(indexer.GetValue(it, new object[] { attrName }));
                    continue;
                }
            }
            catch
            {
                // ignore
            }

            result.Add(null);
        }

        return result;
    }

    private static object DoSelectAttr(object value, object[] args)
    {
        if (value == null) return new List<object>();
        if (!(value is IEnumerable enumerable) || value is string) return value;

        var items = enumerable.Cast<object>();

        // Expect positional args: attributeName, testName (e.g. 'eq'/'equalto'), compareValue
        string? attrName = args.Length > 0 ? args[0]?.ToString() : null;
        var testName = args.Length > 1 ? args[1]?.ToString() : "equalto";
        var compareValue = args.Length > 2 ? args[2] : null;

        if (string.IsNullOrEmpty(attrName)) return items.ToList();

        bool Test(object? val)
        {
            if (testName == null) return Equals(val, compareValue);
            var tn = testName.ToLowerInvariant();
            return tn switch
            {
                "eq" => Equals(val, compareValue),
                "equalto" => Equals(val, compareValue),
                "equal" => Equals(val, compareValue),
                _ => Equals(val, compareValue)
            };
        }

        var result = new List<object>();
        foreach (var it in items)
        {
            object? attrVal = null;
            if (it == null)
            {
                attrVal = null;
            }
            else if (it is System.Collections.IDictionary dict)
            {
                attrVal = dict.Contains(attrName) ? dict[attrName] : null;
            }
            else
            {
                var t = it.GetType();
                var prop = t.GetProperty(attrName);
                if (prop != null) attrVal = prop.GetValue(it);
                else
                {
                    var field = t.GetField(attrName);
                    if (field != null) attrVal = field.GetValue(it);
                }
            }

            if (Test(attrVal)) result.Add(it);
        }

        return result;
    }

    private static string DoJoinFilter(object value, object[] args)
    {
        if (value is not IEnumerable enumerable || value is string)
        {
            return value?.ToString() ?? EmptyString;
        }

        var separator = args.Length > 0 ? args[0]?.ToString() ?? EmptyString : EmptyString;
        var stringItems = enumerable.Cast<object>().Select(x => x?.ToString() ?? EmptyString).ToArray();
        return string.Join(separator, stringItems);
    }

    private static object DoLastFilter(object value)
    {
        if (value is IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>().LastOrDefault();
        }

        return value;
    }

    private static string DoReplaceFilter(string value, object[] args)
    {
        if (string.IsNullOrEmpty(value) || args.Length < 2)
        {
            return value;
        }

        return value.Replace(args[0]?.ToString() ?? EmptyString, args[1]?.ToString() ?? EmptyString);
    }

    private static object DoReverseFilter(object value)
    {
        return value switch
        {
            string s => new string(s.Reverse().ToArray()),
            IEnumerable e => e.Cast<object>().Reverse().ToList(),
            _ => value
        };
    }

    private static object DoSortFilter(object value)
    {
        if (value is IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>().OrderBy(x => x?.ToString()).ToList();
        }

        return value;
    }

    private static int GetLength(object value)
    {
        return value switch
        {
            string s => s.Length,
            ICollection c => c.Count,
            IEnumerable e => e.Cast<object>().Count(),
            _ => 0
        };
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
    }
}