using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snail.Utilities.Collections.Utils;
/// <summary>
/// <see cref="IDictionary{TKey, TValue}"/>助手类
/// </summary>
public static class DictionaryHelper
{
    #region 公共方法

    #region 数据验证
    /// <summary>
    /// 【字典】为null或者空时抛出异常
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <returns>不抛出异常时返回<paramref name="value"/>自身，方便做链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>为null或者空时抛出</exception>
    public static IDictionary<TKey, TValue> ThrowIfNullOrEmpty<TKey, TValue>(IDictionary<TKey, TValue>? value, string? message = "cannot null or empty",
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TKey : notnull
    {
        if (value == null || value.Count == 0)
        {
            throw BuildArgNullException(message, paramName);
        }
        return value;
    }
    /// <summary>
    /// 【字典】非null或者非空时抛出异常
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"><paramref name="value"/>长度大于0时抛出</exception>
    public static void ThrowIfNotNullOrEmpty<TKey, TValue>(IDictionary<TKey, TValue>? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TKey : notnull
    {
        if (value != null && value.Count > 0)
        {
            throw BuildArgException(message, paramName);
        }
    }

    /// <summary>
    /// 是null或者空字典
    /// </summary>
    /// <typeparam name="Tkey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<Tkey, TValue>([NotNullWhen(false)] IDictionary<Tkey, TValue>? value)
        => value == null || value.Count == 0;
    #endregion

    #endregion
}
