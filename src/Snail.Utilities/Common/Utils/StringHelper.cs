using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common.Utils;
/// <summary>
/// <see cref="string"/>助手类
/// </summary>
public static class StringHelper
{
    #region 公共方法

    #region 数据验证
    /// <summary>
    /// 字符串默认值：null或者空给默认值，否则保持原样输出
    /// </summary>
    /// <param name="str"></param>
    /// <param name="defaultStr"></param>
    /// <returns></returns>
    public static string? Default(in string? str, in string? defaultStr)
        => str?.Length > 0 ? str : defaultStr;

    /// <summary>
    /// 字符串是否是null或者空
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsNullOrEmpty(string? value)
        => value == null || value.Length == 0;

    /// <summary>
    /// 字符串为null或者空时抛出异常
    /// </summary>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <returns>返回<paramref name="value"/>自身，方便链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>为null或者空时抛出</exception>
    public static string ThrowIfNullOrEmpty(string? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == null || value.Length == 0)
        {
            throw BuildArgNullException(message, paramName);
        }
        return value;
    }
    /// <summary>
    /// 字符串非null或者非空时抛出异常
    /// </summary>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <returns>非null时返回对象自身，方便做链式调用</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/>长度大于0时抛出</exception>
    public static void ThrowIfNotNullOrEmpty(string? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value != null && value.Length > 0)
        {
            throw BuildArgException(message, paramName);
        }
    }
    #endregion

    #endregion
}
