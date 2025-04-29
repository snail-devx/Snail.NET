using System.Runtime.CompilerServices;

namespace Snail.Utilities.IO.Utils
{
    /// <summary>
    /// 目录助手类
    /// </summary>
    public static class DirectoryHelper
    {
        #region 公共方法

        #region 数据验证
        /// <summary>
        /// 目录不存在时抛出异常
        /// </summary>
        /// <param name="directory">目录路径；非null、非空</param>
        /// <param name="message">异常消息，根据需要自己传递</param>
        /// <param name="paramName">参数名，外部默认null即可，内部自动转换</param>
        /// <exception cref="ArgumentNullException">key为空</exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static void ThrowIfNotFound(string directory, string? message = null, [CallerArgumentExpression(nameof(directory))] string? paramName = null)
        {
            ThrowIfNullOrEmpty(directory, message, paramName);
            if (Directory.Exists(directory) == false)
            {
                message = Default(message, directory);
                throw new DirectoryNotFoundException(message);
            }
        }
        #endregion

        #endregion
    }
}
