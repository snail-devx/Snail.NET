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

    /// <summary>
    ///  进行消息处理时，禁用消息中间件
    /// <para>1、为true时，发送/接收消息时不执行配置好的消息中间件</para>
    /// <para>2、满足在一些特定业务场景下，无需中间件处理消息，直接原生对接消息服务器</para>
    /// </summary>
    bool DisableMiddleware { get; }
}
