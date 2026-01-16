using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.Extensions;
/// <summary>
/// 消息配置选项扩展方法
/// </summary>
public static class MessageOptionsExtensions
{
    #region 扩展方法
    /// <summary>
    /// 获取配置选项字符串
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string GetString(this IMessageOptions options)
    {
        return $"Exchange={options.Exchange} Routing={options.Routing}";
    }
    /// <summary>
    /// 获取配置选项字符串
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string GetString(this ISendOptions options)
    {
        return $"Exchange={options.Exchange} Routing={options.Routing} DisableMiddleware={options.DisableMiddleware}";
    }
    /// <summary>
    /// 获取配置选项字符串
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string GetString(this IReceiveOptions options)
    {
        return $"Exchange={options.Exchange} Routing={options.Routing} Queue={options.Queue} Attempt={options.Attempt} Concurrent={options.Concurrent} DisableMiddleware={options.DisableMiddleware}";
    }
    #endregion
}