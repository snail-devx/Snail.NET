namespace Snail.Abstractions.Message.Enumerations;

/// <summary>
/// 枚举：消息类型
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 队列消息
    /// <para>1、消息只会推送给一个接收者 </para>
    /// <para>2、参照RabbitMQ的Direct消息 </para>
    /// </summary>
    MQ = 10,

    /// <summary>
    /// 发布订阅消息
    /// <para>1、消息会推送给所有接收者 </para>
    /// <para>2、参照RabbitMQ的Fanout消息 </para>
    /// </summary>
    PubSub = 20,
}
