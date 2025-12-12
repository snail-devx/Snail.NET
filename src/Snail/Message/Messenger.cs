using Snail.Abstractions.Message;
using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Message;

/// <summary>
/// 消息信差、负责进行消息的投递和派送<br />
///     1、发送消息到指定的消息服务器<br />
///     2、从指定消息服务器接收消息
/// </summary>
[Component<IMessenger>(Lifetime = LifetimeType.Transient)]
public sealed class Messenger : IMessenger
{
    #region 属性变量
    /// <summary>
    /// 消息管理器
    /// </summary>
    private readonly IMessageManager _manager;
    /// <summary>
    /// 消息服务器
    /// </summary>
    private readonly IServerOptions _server;
    /// <summary>
    /// 消息提供程序
    /// </summary>
    private readonly IMessageProvider _provider;

    /// <summary>
    /// 消息发送器委托
    /// </summary>
    private readonly SendDelegate _sender;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">消息服务器配置选项</param>
    /// <param name="provider">消息提供程序，为nulll则采用默认的</param>
    public Messenger(IApplication app, IServerOptions server, IMessageProvider? provider = null)
    {
        _manager = app.ResolveRequired<IMessageManager>();
        _server = ThrowIfNull(server);
        _provider = provider = provider ??= app.ResolveRequired<IMessageProvider>();
        //  这里将消息发送器做一下构建，不用每次发送时都构建（但若单纯只是接收消息，这里构建就有点浪费，后期再优化）
        _sender = _manager.Build((SendDelegate)_provider.Send);
    }
    #endregion

    #region IMessenger
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="type">消息类型：mq、pubsub、、、</param>
    /// <param name="message">消息描述器：</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <returns>发送成功，返回true；否则false</returns>
    Task<bool> IMessenger.Send(MessageType type, MessageDescriptor message, IMessageOptions options)
    {
        //  若消息配置显示指定了不启用中间件，则直接发送
        ThrowIfNull(message);
        ThrowIfNull(options);
        return options.DisableMiddleware
            ? _provider.Send(type, message, options, _server)
            : _sender.Invoke(type, message, options, _server);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="receiver">消息接收器；用于处理具体消息</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
    /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
    Task<bool> IMessenger.Receive(MessageType type, Func<MessageDescriptor, Task<bool>> receiver, IReceiveOptions options)
    {
        //  若消息配置显示指定了不启用中间件，则直接接收
        ThrowIfNull(receiver);
        ThrowIfNull(options);
        if (options.DisableMiddleware != true)
        {
            receiver = BuildReceiverWithMiddleware(type, receiver, options);
        }
        return _provider.Receive(type, receiver, options, _server);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建包含【消息中间件】的消息接收器
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="receiver">消息接收器；用于处理具体消息</param>
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机、重视次数等信息</param>
    /// <returns>消息接收器注册成功，返回true；否则返回false</returns>
    private Func<MessageDescriptor, Task<bool>> BuildReceiverWithMiddleware(MessageType type, Func<MessageDescriptor, Task<bool>> receiver, IReceiveOptions options)
    {
        ReceiveDelegate handle = async (type, message, options, server) => await receiver.Invoke(message);
        handle = _manager.Build(handle);
        return message => handle.Invoke(type, message, options, _server);
    }
    #endregion
}
