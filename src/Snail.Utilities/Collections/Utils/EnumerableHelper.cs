using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Snail.Utilities.Collections.Utils;
/// <summary>
/// <see cref="IEnumerable{T}"/>助手类
/// </summary>
public static class EnumerableHelper
{
    #region 公共方法

    #region 数据验证
    /// <summary>
    /// 【可枚举对象】为null或者空时抛出异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <returns>不抛出异常时返回<paramref name="value"/>自身，方便做链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>为null或者空时抛出</exception>
    public static IEnumerable<T> ThrowIfNullOrEmpty<T>(IEnumerable<T>? value, string? message = "cannot null or empty",
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == null || value.Any() != true)
        {
            throw BuildArgNullException(message, paramName);
        }
        return value;
    }
    /// <summary>
    /// 【可枚举对象】非null或者非空时抛出异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"><paramref name="value"/>长度大于0时抛出</exception>
    public static void ThrowIfNotNullOrEmpty<T>(IEnumerable<T>? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value != null && value.Any() == true)
        {
            throw BuildArgException(message, paramName);
        }
    }
    /// <summary>
    /// 是null或者空字典
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] IEnumerable<T>? value)
        => value == null || value.Any();
    #endregion

    #endregion
}
