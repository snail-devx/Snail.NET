using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common.Utils;
/// <summary>
/// <see cref="Exception"/>助手类
/// </summary>
public static class ExceptionHelper
{
    #region Throw 条件
    /// <summary>
    /// <paramref name="ex"/>非空时，抛出异常
    /// </summary>
    /// <param name="ex"></param>
    public static void TryThrow(Exception? ex)
    {
        if (ex != null)
        {
            throw ex;
        }
    }

    /// <summary>
    /// 条件成立时，抛出异常
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="ex"></param>
    public static void ThrowIf(bool condition, Exception ex)
    {
        if (condition == true)
        {
            throw ex;
        }
    }

    /// <summary>
    /// 值为true时抛出异常
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIf(bool condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        if (condition == true)
        {
            throw BuildArgException(message, paramName);
        }
    }
    /// <summary>
    /// 值为true时抛出异常
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIf(bool? condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        if (condition == true)
        {
            throw BuildArgException(message, paramName);
        }
    }
    /// <summary>
    /// 值不为null/false时抛出异常：
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIfNotTrue(bool? condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        //  Boolean类型不提供 NotTrue，若需要直接使用IsFalse即可
        if (condition != true)
        {
            throw BuildArgException(message, paramName);
        }
    }

    /// <summary>
    /// 值为false时抛出异常
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIfFalse(bool condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        if (condition == false)
        {
            throw BuildArgException(message, paramName);
        }
    }
    /// <summary>
    /// 值为false时抛出异常
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIfFalse(bool? condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        if (condition == false)
        {
            throw BuildArgException(message, paramName);
        }
    }
    /// <summary>
    /// 值不为null/true时抛出异常：
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="message"></param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentException"></exception>
    public static void ThrowIfNotFalse(bool? condition, string? message = null, [CallerArgumentExpression("condition")] string? paramName = null)
    {
        //  Boolean类型不提供 NotFlase，若需要直接使用IsTrue即可
        if (condition != false)
        {
            throw BuildArgException(message, paramName);
        }
    }
    #endregion
}
