using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Message.Delegates;

/// <summary>
/// 消息委托：发送消息
/// </summary>
/// <param name="type">消息类型：如mq、pubsub</param>
/// <param name="message">接收/发送的消息数据</param>
/// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
/// <param name="server">消息服务器地址：接收的消息来自哪里；消息发送哪里</param>
/// <returns>处理使用成功，成功返回true，否则返回false</returns>
public delegate Task<bool> SendDelegate(MessageType type, MessageDescriptor message, ISendOptions options, IServerOptions server);