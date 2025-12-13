namespace Snail.Abstractions.Message.Interfaces;

/// <summary>
/// 接口约束：消息接收配置选项
/// </summary>
public interface IReceiveOptions : IMessageOptions
{
    /// <summary>
    /// 接收消息的队列名称
    /// </summary>
    string Queue { get; }

    /// <summary>
    /// 接收消息的尝试次数
    /// <para>1、接收方发生异常后，尝试多少次后仍失败，则强制确认，避免消息堆积 </para>
    /// <para>2、&lt;= 0 不自动确认；直到处理成功 </para>
    /// </summary>
    int Attempt { get; }

    /// <summary>
    /// 消息接收器并发数量
    /// <para>1、当前接收器从<see cref="Queue"/>接收消息的并发量 </para>
    /// <para>2、大于1时生效，合理设置，提高消息消费效率 </para>
    /// </summary>
    int Concurrent { get; }

    //7、是否弹性：
}
