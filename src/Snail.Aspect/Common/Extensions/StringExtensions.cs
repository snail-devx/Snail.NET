using System.Security.Cryptography;
using System.Text;

namespace Snail.Aspect.Common.Extensions;

/// <summary>
/// 字符串的扩展方法
/// </summary>
internal static class StringExtensions
{
    #region 公共方法
    /// <summary>
    /// 对字符串进行MD5运算
    /// </summary>
    /// <param name="str"></param>
    /// <param name="needStrikethrough">是否需要md5值的中划线</param>
    /// <returns></returns>
    public static string AsMD5(this string str, in bool needStrikethrough = false)
    {
        byte[] data = MD5.HashData(Encoding.Default.GetBytes(str));// MD5.Create().ComputeHash(Encoding.Default.GetBytes(str));
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
}
