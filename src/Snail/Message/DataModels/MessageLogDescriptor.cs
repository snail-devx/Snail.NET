using Snail.Abstractions.Logging.DataModels;

namespace Snail.Message.DataModels;

/// <summary>
/// 消息发送日志描述器
/// </summary>
/// <remarks>
/// 构造方法
/// </remarks>
/// <param name="isForce"></param>
public sealed class MessageSendLogDescriptor(bool isForce) : LogDescriptor(isForce), IIdentifiable
{
    #region 属性变量
    /// <summary>
    /// 日志操作Id值
    /// </summary>
    public required string Id { get; init; }
    /// <summary>
    /// 消息发送方服务器
    /// </summary>
    public required string ServerOptions { get; init; }
    #endregion
}

/// <summary>
/// 消息接收日志描述器
/// </summary>
public sealed class MessageReceiveLogDescriptor : LogDescriptor, IPerformance
{
    /// <summary>
    /// 接收的消息来自哪里
    /// </summary>
    public required string ServerOptions { get; init; }

    /// <summary>
    /// 请求耗时毫秒
    /// </summary>
    public long? Performance { set; get; }
}
