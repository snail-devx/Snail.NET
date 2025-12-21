using System.Collections.Generic;
using System.Linq;

namespace Snail.Aspect.Common.Extensions;

/// <summary>
/// <see cref="List{T}"/>扩展方法
/// </summary>
internal static class ListExtensions
{
    #region 公共方法
    /// <summary>
    /// 往list添加数据，为null不加
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="lst"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IList<T> TryAdd<T>(this IList<T> lst, T value)
    {
        if (value != null)
        {
            lst.Add(value);
        }
        return lst;
    }
    /// <summary>
    /// 往list添加数据，为null、空不加
    /// </summary>
    /// <param name="lst"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static IList<string> TryAdd(this IList<string> lst, string value)
    {
        if (value?.Length > 0)
        {
            lst.Add(value);
        }
        return lst;
    }

    /// <summary>
    /// 尝试批量添加数据；<paramref name="list"/>为空则不执行
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    public static List<T> TryAddRange<T>(this List<T> source, IEnumerable<T>? list)
    {
        if (list?.Any() == true)
        {
            source.AddRange(list);
        }
        return source;
    }
    #endregion
}
