using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Message.Interfaces;

/// <summary>
/// 接口约束：发送消息中间件
/// </summary>
public interface ISendMiddleware
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="message">发送的消息数据</param>        
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <param name="server">消息服务器地址消息发送哪里</param>
    /// <param name="next">下一个消息处理委托</param>
    /// <returns></returns>
    Task<bool> Send(MessageType type, MessageDescriptor message, IMessageOptions options, IServerOptions server, SendDelegate next);
}
/// <summary>
/// 接口约束：接收消息中间件
/// </summary>
public interface IReceiveMiddleware
{
    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="message">发送的消息数据</param>        
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
    /// <param name="next">下一个消息处理委托</param>
    /// <returns></returns>
    Task<bool> Receive(MessageType type, MessageDescriptor message, IReceiveOptions options, IServerOptions server, ReceiveDelegate next);
}

/// <summary>
/// 接口约束：消息中间件；发送+接收消息
/// </summary>
public interface IMessageMiddleware : ISendMiddleware, IReceiveMiddleware
{
}
