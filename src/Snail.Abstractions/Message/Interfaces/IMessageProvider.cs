using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Message.Interfaces;

/// <summary>
/// 接口约束：消息提供程序，和具体的消息中间件（如RabbitMQ）打交道
/// </summary>
public interface IMessageProvider
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="message">发送的消息数据</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <param name="server">消息服务器地址：消息发送哪里</param>
    /// <returns>处理使用成功，成功返回true，否则返回false</returns>
    Task<bool> Send<T>(MessageType type, T message, IMessageOptions options, IServerOptions server);

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="receiver">消息接收器；用于处理具体消息；接收到消息后，执行此委托</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
    /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
    /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
    Task<bool> Receive<T>(MessageType type, Func<T, Task<bool>> receiver, IReceiveOptions options, IServerOptions server);
}
