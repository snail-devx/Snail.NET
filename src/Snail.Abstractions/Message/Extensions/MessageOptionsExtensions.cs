using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.Extensions;
/// <summary>
/// 消息配置选项扩展方法
/// </summary>
public static class MessageOptionsExtensions
{
    #region IMessageOptions
    extension(IMessageOptions options)
    {
        /// <summary>
        /// 获取配置选项字符串
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            return $"Exchange={options.Exchange} Routing={options.Routing} DisableMiddleware={options.DisableMiddleware}";
        }
    }
    #endregion

    #region IReceiveOptions
    extension(IReceiveOptions options)
    {
        /// <summary>
        /// 获取配置选项字符串
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            return $"Exchange={options.Exchange} Routing={options.Routing} Queue={options.Queue} Attempt={options.Attempt} Concurrent={options.Concurrent} DisableMiddleware={options.DisableMiddleware}";
        }
    }
    #endregion
}