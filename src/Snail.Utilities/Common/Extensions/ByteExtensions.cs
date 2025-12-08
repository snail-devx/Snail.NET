using System.Text;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// <see cref="byte"/>扩展方法
/// </summary>
public static class ByteExtensions
{
    #region 公共方法

    #region String
    /// <summary>
    /// byte数组转成字符串
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="encoding">默认UTF8编码</param>
    /// <returns></returns>
    public static string AsString(this byte[] bytes, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return encoding.GetString(bytes);
    }
    #endregion

    #endregion
}
