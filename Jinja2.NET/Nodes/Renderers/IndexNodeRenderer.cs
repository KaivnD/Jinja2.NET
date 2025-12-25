using System.Collections;
using Jinja2.NET.Interfaces;
using Jinja2.NET.Nodes;

namespace Jinja2.NET.Nodes.Renderers;

public class IndexNodeRenderer : INodeRenderer
{
    public object? Render(ASTNode nodeIn, IRenderer renderer)
    {
        if (nodeIn is not IndexNode node)
        {
            throw new ArgumentException($"Expected IndexNode, got {nodeIn.GetType().Name}");
        }
        var target = renderer.Visit(node.Target);

        if (target == null)
        {
            return null;
        }

        if (node.Index is SliceNode slice)
        {
            int? startRaw = null;
            int? stopRaw = null;
            int? stepRaw = null;

            object? startObj = slice.Start != null ? renderer.Visit(slice.Start) : null;
            object? stopObj = slice.Stop != null ? renderer.Visit(slice.Stop) : null;
            object? stepObj = slice.Step != null ? renderer.Visit(slice.Step) : null;

            int? ToInt(object? o)
            {
                if (o == null) return null;
                if (o is int i) return i;
                if (o is long l) return (int)l;
                if (o is double dd) return (int)dd;
                if (o is string s && int.TryParse(s, out var parsed)) return parsed;
                throw new InvalidOperationException($"Slice indices must be integers or empty, got {o.GetType().Name}");
            }

            startRaw = ToInt(startObj);
            stopRaw = ToInt(stopObj);
            stepRaw = ToInt(stepObj) ?? 1;

            if (stepRaw == 0)
            {
                throw new InvalidOperationException("slice step cannot be zero");
            }

            // Support IList, Array and string
            if (target is string str)
            {
                var len = str.Length;
                var step = stepRaw.Value;

                int NormalizeIndexForString(int? v, bool isStart, int stepSign)
                {
                    if (v == null)
                    {
                        return isStart ? (stepSign > 0 ? 0 : len - 1) : (stepSign > 0 ? len : -1);
                    }
                    var x = v.Value;
                    if (x < 0) x += len;
                    return x;
                }

                var start = NormalizeIndexForString(startRaw, true, Math.Sign(step));
                var stop = NormalizeIndexForString(stopRaw, false, Math.Sign(step));

                var chars = new List<char>();
                if (step > 0)
                {
                    for (int i = start; i < stop && i < len; i += step)
                    {
                        if (i >= 0 && i < len) chars.Add(str[i]);
                    }
                }
                else
                {
                    for (int i = start; i > stop && i >= 0; i += step)
                    {
                        if (i >= 0 && i < len) chars.Add(str[i]);
                    }
                }

                return new string(chars.ToArray());
            }

            if (target is IList list)
            {
                var len = list.Count;
                var step = stepRaw.Value;

                int NormalizeIndex(int? v, bool isStart)
                {
                    if (v == null) return isStart ? (step > 0 ? 0 : len - 1) : (step > 0 ? len : -1);
                    var x = v.Value;
                    if (x < 0) x += len;
                    return x;
                }

                var start = NormalizeIndex(startRaw, true);
                var stop = NormalizeIndex(stopRaw, false);

                var results = new List<object?>();
                if (step > 0)
                {
                    for (int i = start; i < stop && i < len; i += step)
                    {
                        if (i >= 0 && i < len) results.Add(list[i]);
                    }
                }
                else
                {
                    for (int i = start; i > stop && i >= 0; i += step)
                    {
                        if (i >= 0 && i < len) results.Add(list[i]);
                    }
                }

                return results;
            }

            if (target is Array arr)
            {
                var resultList = new List<object?>();
                var len = arr.Length;
                var step = stepRaw.Value;

                int NormalizeIndex(int? v, bool isStart)
                {
                    if (v == null) return isStart ? (step > 0 ? 0 : len - 1) : (step > 0 ? len : -1);
                    var x = v.Value;
                    if (x < 0) x += len;
                    return x;
                }

                var start = NormalizeIndex(startRaw, true);
                var stop = NormalizeIndex(stopRaw, false);

                if (step > 0)
                {
                    for (int i = start; i < stop && i < len; i += step)
                    {
                        if (i >= 0 && i < len) resultList.Add(arr.GetValue(i));
                    }
                }
                else
                {
                    for (int i = start; i > stop && i >= 0; i += step)
                    {
                        if (i >= 0 && i < len) resultList.Add(arr.GetValue(i));
                    }
                }

                return resultList;
            }

            throw new InvalidOperationException($"Cannot slice type '{target?.GetType().Name}'");
        }

        // 非切片，原有单索引逻辑
        var index = renderer.Visit(node.Index);

        // Convert double index to int if possible
        if (index is double d && d % 1 == 0)
        {
            index = (int)d;
        }

        if (target is IDictionary dict)
        {
            try
            {
                return dict.Contains(index) ? dict[index] : null;
            }
            catch
            {
                return null;
            }
        }

        if (target is IList listSingle && index is int idx)
        {
            // Support negative indices (Python-like)
            var actual = idx;
            if (actual < 0)
            {
                actual = listSingle.Count + actual;
            }
            if (actual < 0 || actual >= listSingle.Count)
            {
                return null;
            }
            return listSingle[actual];
        }

        if (target is Array arrSingle)
        {
            if (index is int arrIdx)
            {
                var actual = arrIdx;
                if (actual < 0)
                {
                    actual = arrSingle.Length + actual;
                }
                if (actual < 0 || actual >= arrSingle.Length)
                {
                    return null;
                }
                return arrSingle.GetValue(actual);
            }

            throw new InvalidOperationException($"Array index must be int, got {index?.GetType().Name}");
        }

        if (target is string strSingle && index is int strIdx)
        {
            var actual = strIdx;
            if (actual < 0) actual = strSingle.Length + actual;
            if (actual >= 0 && actual < strSingle.Length)
            {
                return strSingle[actual].ToString();
            }
            return null;
        }

        throw new InvalidOperationException($"Cannot index into type '{target?.GetType().Name}' with '{index}'.");
    }
}