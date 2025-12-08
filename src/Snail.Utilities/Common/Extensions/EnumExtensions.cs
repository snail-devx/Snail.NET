using System.Runtime.CompilerServices;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// 枚举扩展方法
/// </summary>
public static class EnumExtensions
{
    #region 公共方法
    /// <summary>
    /// 检测枚举值是否有效
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="enum"></param>
    /// <returns></returns>
    public static bool IsValid<TEnum>(this TEnum @enum) where TEnum : struct, Enum
        => Enum.IsDefined<TEnum>(@enum);

    /// <summary>
    /// 检测枚举值是否有效，不在则抛出异常
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="exTitle">异常标题</param>
    /// <remarks>组装的异常消息：“<paramref name="exTitle"/>不是有效的[TEnum]枚举值：<paramref name="value"/>”</remarks>
    public static void Check<TEnum>(this TEnum value, [CallerArgumentExpression("value")] string? exTitle = null) where TEnum : struct, Enum
    {
        if (Enum.IsDefined(value) != true)
        {
            string msg = exTitle?.Length > 0 ? $"{exTitle}不是有效的[{nameof(TEnum)}]枚举值：{value}" : $"不是有效的[{nameof(TEnum)}]枚举值：{value}";
            throw new ArgumentException(msg);
        }
    }

    /// <summary>
    /// 获取枚举的字符串值
    ///     如枚举定义：Success=0；则返回Success
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AsString(this Enum value)
        => value.ToString();
    /// <summary>
    /// 枚举转Int值
    ///     如枚举定义：Success=100；则返回100
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int AsInt32(this Enum value)
        => Convert.ToInt32(value);
    #endregion
}
