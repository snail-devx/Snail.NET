using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message.DataModels;

/// <summary>
/// 消息接收配置选项
/// </summary>
public sealed class ReceiveOptions : MessageOptions, IReceiveOptions
{
    /** 继承<see cref="IMessageOptions"/>属性
     *      <see cref="MessageOptions.Exchange"/>       交换机名称
     *      <see cref="MessageOptions.Routing"/>        路由
     */

    /// <summary>
    /// 接收消息的队列名称
    /// </summary>
    public required string Queue { init; get; }

    /// <summary>
    /// 接收消息的尝试次数
    /// <para>1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 </para>
    /// <para>2、&lt;= 0 不自动确认；直到处理成功 </para>
    /// </summary>
    public int Attempt { init; get; }

    /// <summary>
    /// 消息接收器并发数量
    /// <para>1、当前接收器从<see cref="Queue"/>接收消息的并发量 </para>
    /// <para>2、大于1时生效，合理设置，提高消息消费效率 </para>
    /// </summary>
    public int Concurrent { init; get; }
}
