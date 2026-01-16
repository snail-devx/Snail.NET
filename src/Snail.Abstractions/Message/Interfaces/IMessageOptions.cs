namespace Snail.Abstractions.Message.Interfaces;

/// <summary>
/// 接口约束：消息配置选项
/// <param>1、参照RabbitMQ机制提取的配置选项；约束队列、路由等相关信息</param>
/// <param>2、根据具体的<see cref="IMessageProvider"/>实现类不同，配置选项不一定都生效</param>
/// </summary>
public interface IMessageOptions
{
    /// <summary>
    /// 消息交换机名称
    /// </summary>
    string? Exchange { get; }
    /// <summary>
    /// 消息交换机和队列之间的路由
    /// </summary>
    string? Routing { get; }
}
