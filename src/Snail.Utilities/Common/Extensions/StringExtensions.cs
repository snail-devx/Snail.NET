using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// <see cref="string"/>的扩展方法
/// </summary>
public static class StringExtensions
{
    #region 等值比较
    /// <summary>
    /// 两个字符串是否相等
    /// </summary>
    /// <param name="str"></param>
    /// <param name="other"></param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public static bool IsEqual(this string str, string? other, bool ignoreCase = false)
        => string.Compare(str, other, ignoreCase) == 0;
    /// <summary>
    /// 两个字符串是否不相等
    /// </summary>
    /// <param name="str"></param>
    /// <param name="other"></param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public static bool NotEqual(this string str, string? other, bool ignoreCase = false)
        => string.Compare(str, other, ignoreCase) != 0;
    #endregion

    #region Any、ForEach
    /// <summary>
    /// 字符串是否有值
    /// <para>1、不是null、不是空字符串 </para>
    /// <para>2、直接使用自身类型属性判断；不用.Any </para>
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool Any(this string str)
        => str.Length != 0;
    #endregion

    #region 转换相关：As、TryAs

    #region Json序列化
    /// <summary>
    /// 类型反序列化；使用<see cref="JsonConvert.DeserializeObject{T}(string)"/>完成
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <param name="settings">json序列化配置</param>
    /// <returns></returns>
    public static T As<T>(this string str, JsonSerializerSettings? settings = null)
        => JsonConvert.DeserializeObject<T>(str, settings)!;
    /// <summary>
    /// 尝试进行类型反序列化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="str"></param>
    /// <param name="value"></param>
    /// <param name="settings">json序列化配置</param>
    /// <returns></returns>
    public static bool TryAs<T>(this string str, out T? value, JsonSerializerSettings? settings = null)
    {
        RunResult<T?> rt = Run(JsonConvert.DeserializeObject<T>, str, settings);
        value = rt.Data;
        return rt;
    }
    #endregion

    #region Regex
    /// <summary>
    /// 将字符串转换成正则表达式
    /// </summary>
    /// <param name="str">正则表达式字符串，确保非null、空，否则构建返回null</param>
    /// <param name="options">正则匹配选项</param>
    /// <param name="needEscape">是否需要调用<see cref="Regex.Escape(string)"/>做编码</param>
    /// <returns><paramref name="str"/>为null、空时返回null；否则为正则表达式 </returns>
    public static Regex? AsRegex(this string? str, in RegexOptions options = RegexOptions.None, in bool needEscape = false)
    {
        return str?.Length > 0
            ? new Regex(needEscape ? Regex.Escape(str) : str, options)
            : null;
    }
    #endregion

    #region Enum
    /// <summary>
    /// 将字符串转换成指定枚举值
    ///     若转换失败，则报错
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="str">要转换的字符串，确保在枚举定义的Key中</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    public static TEnum AsEnum<TEnum>(this string str, in bool ignoreCase = false) where TEnum : struct, Enum
        => Enum.Parse<TEnum>(str, ignoreCase);
    /// <summary>
    /// 字符串是否是指定类型枚举
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool IsEnum<TEnum>(this string str) where TEnum : struct, Enum
        => Enum.IsDefined(typeof(TEnum), str);
    /// <summary>
    /// 字符串是否是指定类型枚举
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="str">要转换的字符串，确保在枚举定义的Key中</param>
    /// <param name="value">out参数；若不是有效枚举，则返回默认值0</param>
    /// <returns></returns>
    public static bool IsEnum<TEnum>(this string str, out TEnum value) where TEnum : struct, Enum
        => Enum.TryParse(str, out value);
    #endregion

    #region Byte
    /// <summary>
    /// 将字符串转换成Byte数组
    /// </summary>
    /// <param name="str">要转成Byte[]的字符串</param>
    /// <param name="encoding">默认UTF8编码</param>
    /// <returns></returns>
    public static byte[] AsBytes(this string str, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetBytes(str);
    }
    #endregion

    #region MD5
    /// <summary>
    /// 对字符串进行MD5运算
    /// </summary>
    /// <param name="str"></param>
    /// <param name="needStrikethrough">是否需要md5值的中划线</param>
    /// <returns></returns>
    public static string AsMD5(this string str, in bool needStrikethrough = false)
    {
        byte[] data = MD5.Create().ComputeHash(Encoding.Default.GetBytes(str));
        StringBuilder sBuilder = new();
        for (int i = 0; i < data.Length; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }
        //  返回
        if (needStrikethrough == false)
        {
            sBuilder = sBuilder.Replace("-", string.Empty);
        }
        return sBuilder.ToString();
    }
    #endregion

    #region Base64
    /// <summary>
    /// Base64编码
    /// </summary>
    /// <param name="str"></param>
    /// <param name="options">是否在其输出中插入换行符；默认不插入</param>
    /// <returns></returns>
    public static string AsBase64Encode(this string str, in Base64FormattingOptions options = Base64FormattingOptions.None)
    {
        byte[] bytes = str.AsBytes();
        return Convert.ToBase64String(bytes, options);
    }
    /// <summary>
    /// Base64解码
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string AsBase64Decode(this string str)
    {
        byte[] bytes = Convert.FromBase64String(str);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Base64Url编码
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string AsBase64UrlEncode(this string? str)
    {
        //  微软提供了内置实现；但在AspNetCore中；此类库不依赖此库，这里做一些简化实现。Microsoft.AspNetCore.WebUtilities.WebEncoders
        //  这种方式，性能不是最优解，后续考虑把微软默认实现放到这里来《里面有一些span操作》
        ThrowIfNullOrEmpty(str);
        str = str!.AsBase64Encode()
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return str;
    }
    #endregion

    #region URL
    /// <summary>
    /// URL编码
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string AsUrlEncode(this string str)
        => HttpUtility.UrlEncode(str);
    /// <summary>
    /// URL解码
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string AsUrlDecode(this string str)
        => HttpUtility.UrlDecode(str);
    #endregion

    #region Number
    /// <summary>
    /// 字符串转Int32值
    /// </summary>
    /// <param name="str"></param>
    /// <returns>转换成功返回具体值，否则返回null</returns>
    public static int? AsInt32(this string str)
    {
        return int.TryParse(str, out int ret) == true
            ? ret
            : null;
    }
    /// <summary>
    /// 字符串转Int64值
    /// </summary>
    /// <param name="str"></param>
    /// <returns>转换成功返回具体值，否则返回null</returns>
    public static long? AsInt64(this string str)
    {
        return long.TryParse(str, out long ret) == true
            ? ret
            : null;
    }
    /// <summary>
    /// 字符串转Double值
    /// </summary>
    /// <param name="str"></param>
    /// <returns>转换成功返回具体值，否则返回null</returns>
    public static double? AsDouble(this string str)
    {
        return double.TryParse(str, out double ret) == true
            ? ret
            : null;
    }
    /// <summary>
    /// 字符串转Decimal
    /// </summary>
    /// <param name="str"></param>
    /// <returns>转换成功返回具体值，否则返回null</returns>
    public static decimal? AsDecimal(this string str)
    {
        return decimal.TryParse(str, out decimal ret) == true
            ? ret
            : null;
    }
    #endregion

    #region DateTime
    /// <summary>
    /// 字符串转日期时间
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static DateTime? AsDateTime(this string str)
    {
        return DateTime.TryParse(str, out DateTime dt) == true
            ? dt
            : null;
    }
    #endregion

    #endregion

    #region 验证处理
    /// <summary>
    /// 对超出长度的文本部分做省略号处理
    /// </summary>
    /// <param name="str"></param>
    /// <param name="maxLength">最大长度，超过此长度的文本使用“...”替换</param>
    /// <returns></returns>
    public static string Ellipsis(this string str, int maxLength)
    {
        return str != null && str.Length > maxLength
            ? $"{str.Substring(0, maxLength)}..."
            : str!;
    }
    #endregion
}
