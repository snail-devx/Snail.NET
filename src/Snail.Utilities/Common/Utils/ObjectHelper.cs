using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common.Utils;
/// <summary>
/// <see cref="object"/>或者T相关助手类
/// </summary>
public static class ObjectHelper
{
    #region 公共方法

    #region 数据验证
    /// <summary>
    /// 【对象】为null时抛出异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <returns>不抛出异常时返回<paramref name="value"/>自身，方便做链式调用</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/>为null时抛出</exception>
    public static T ThrowIfNull<T>(T? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == null)
        {
            throw BuildArgNullException(message, paramName);
        }
        return value;
    }
    /// <summary>
    /// 【对象】非null时抛出异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value">要判断的数据</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"><paramref name="value"/>非null时抛出</exception>
    public static void ThrowIfNotNull<T>(T? value, string? message = null,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value != null)
        {
            throw BuildArgException(message, paramName);
        }
    }
    #endregion

    #endregion

    #region 内部方法
    /// <summary>
    /// 构建<see cref="ArgumentNullException"/>异常对象
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="paramName">参数名称</param>
    /// <returns>异常对象</returns>
    internal static ArgumentNullException BuildArgNullException(string? message, string? paramName)
    {
        /*不同值的情况构造方法不一样 */

        //  message非空
        if (message?.Length > 0)
        {
            return paramName?.Length > 0
                ? new ArgumentNullException(paramName, message)
                : new ArgumentNullException(message, innerException: null);
        }
        //  message空
        else
        {
            return paramName?.Length > 0
                ? new ArgumentNullException(paramName)
                : new ArgumentNullException();
        }
    }
    /// <summary>
    /// 构建<see cref="ArgumentException"/>异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="paramName">抛出异常的参数名</param>
    /// <returns></returns>
    internal static ArgumentException BuildArgException(string? message, string? paramName)
    {
        /*不同值的情况构造方法不一样 */
        //  paramName非空，必须使用 message、paramName构造方法，没有其他的
        return paramName?.Length > 0
            ? new ArgumentException(message, paramName)
            : message?.Length > 0
                ? new ArgumentException(message)
                : new ArgumentException()
            ;
    }
    #endregion
}
