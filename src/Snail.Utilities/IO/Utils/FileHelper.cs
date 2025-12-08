using System.Runtime.CompilerServices;

namespace Snail.Utilities.IO.Utils;
/// <summary>
/// 文件助手类
/// </summary>
public sealed class FileHelper
{
    #region 公共方法

    #region 数据验证
    /// <summary>
    /// 文件不存在时抛出异常
    /// </summary>
    /// <param name="file">文件路径；非null、非空</param>
    /// <param name="message">异常消息，根据需要自己传递</param>
    /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
    /// <exception cref="ArgumentNullException">key为空</exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static void ThrowIfNotFound(string file, string? message = null, [CallerArgumentExpression(nameof(file))] string? paramName = null)
    {
        ThrowIfNullOrEmpty(file, message, paramName);
        if (File.Exists(file) == false)
        {
            throw new FileNotFoundException(message, file);
        }
    }
    #endregion

    #endregion
}
