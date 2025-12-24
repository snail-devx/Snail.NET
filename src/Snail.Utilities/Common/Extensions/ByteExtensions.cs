using System.Text;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// <see cref="byte"/>扩展方法
/// </summary>
public static class ByteExtensions
{
    #region String
    extension(byte[] bytes)
    {
        /// <summary>
        /// byte数组转成字符串
        /// </summary>
        /// <param name="encoding">默认UTF8编码</param>
        /// <returns></returns>
        public string AsString(Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetString(bytes);
        }
    }
    #endregion
}
