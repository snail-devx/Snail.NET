using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;

namespace Snail.Abstractions.Message;

/// <summary>
/// 接口约束：消息信差；负责进行消息的投递和派送
/// <para>1、发送消息到指定的消息服务器 </para>
/// <para>2、从指定消息服务器接收消息 </para>
/// </summary>
public interface IMessenger
{
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="type">消息类型：mq、pubsub、、、</param>
    /// <param name="message">消息描述器：</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <returns>发送成功，返回true；否则false</returns>
    Task<bool> Send(MessageType type, MessageDescriptor message, ISendOptions options);

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="receiver">消息接收器；用于处理具体消息</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
    /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
    Task<bool> Receive(MessageType type, Func<MessageDescriptor, Task<bool>> receiver, IReceiveOptions options);
}
