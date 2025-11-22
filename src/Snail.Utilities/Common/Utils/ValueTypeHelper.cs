using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common.Utils
{
    /// <summary>
    /// 值类型助手类：如bool、int、、、
    /// </summary>
    public static class ValueTypeHelper
    {
        #region 公共方法

        #region bool 相关
        /// <summary>
        /// 【布尔】值为true时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <returns>不抛出异常时返回<paramref name="value"/>自身，方便做链式调用</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/>为true时抛出</exception>
        public static bool ThrowIfTrue(bool value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value == true)
            {
                throw BuildArgException(message, paramName);
            }
            return value;
        }
        /// <summary>
        /// 【布尔】值为true时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <exception cref="ArgumentException"><paramref name="value"/>为true时抛出</exception>
        public static void ThrowIfTrue(bool? value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value == true)
            {
                throw BuildArgException(message, paramName);
            }
        }
        /// <summary>
        /// 【布尔】值不为true时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <exception cref="ArgumentException"><paramref name="value"/>为null或者false时抛出</exception>
        public static void ThrowIfNotTrue(bool? value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            //  NotTrue则可能值为false和null； Boolean类型不提供 NotTrue，若需要直接使用IsFalse即可
            if (value != true)
            {
                throw BuildArgException(message, paramName);
            }
        }

        /// <summary>
        /// 【布尔】值为false时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <returns>不抛出异常时返回<paramref name="value"/>自身，方便做链式调用</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/>为false时抛出</exception>
        public static bool ThrowIfFalse(bool value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value == false)
            {
                throw BuildArgException(message, paramName);
            }
            return value;
        }
        /// <summary>
        /// 【布尔】值为false时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <exception cref="ArgumentException"><paramref name="value"/>为false时抛出</exception>
        public static void ThrowIfFalse(bool? value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            if (value == false)
            {
                throw BuildArgException(message, paramName);
            }
        }
        /// <summary>
        /// 【布尔】值不为true时抛出异常
        /// </summary>
        /// <param name="value">要判断的数据</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <exception cref="ArgumentException"><paramref name="value"/>为null或者true时抛出</exception>
        public static void ThrowIfNotFalse(bool? value, string? message = null,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            //   Boolean类型不提供 NotFalse，若需要直接使用IsFalse即可
            if (value != false)
            {
                throw BuildArgException(message, paramName);
            }
        }
        #endregion

        #endregion
    }
}
