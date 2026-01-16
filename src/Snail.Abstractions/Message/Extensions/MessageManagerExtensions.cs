using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Message.Extensions;

/// <summary>
/// <see cref="IMessageManager"/>扩展方法
/// </summary>
public static class MessageManagerExtensions
{
    #region 公共方法

    #region 消息中间件：发送+接收
    /// <summary>
    /// 使用消息中间件；监听和接收
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, IMessageMiddleware middleware)
        => Use(manager, name: null, middleware);
    /// <summary>
    /// 使用消息中间件；监听和接收
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="name">中间件名称</param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, string? name, IMessageMiddleware middleware)
    {
        ThrowIfNull(middleware);
        Use(manager, name, (ISendMiddleware)middleware);
        Use(manager, name, (IReceiveMiddleware)middleware);
        return manager;
    }
    #endregion

    #region 发送消息 中间件
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, ISendMiddleware middleware)
        => Use(manager, name: null, middleware);
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="name">中间件名称</param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, string? name, ISendMiddleware middleware)
    {
        ThrowIfNull(middleware);
        manager.Use(name, next =>
        {
            Task<bool> sender(MessageType type, MessageDescriptor message, ISendOptions options, IServerOptions server)
                => middleware.Send(type, message, options, server, next);
            return sender;
        });
        return manager;
    }
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, Action<MessageType, MessageDescriptor, ISendOptions, IServerOptions> middleware)
        => Use(manager, name: null, middleware);
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="name">中间件名称</param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, string? name, Action<MessageType, MessageDescriptor, ISendOptions, IServerOptions> middleware)
    {
        ThrowIfNull(middleware);
        manager.Use(name, next =>
        {
            Task<bool> sender(MessageType type, MessageDescriptor message, ISendOptions options, IServerOptions server)
            {
                middleware.Invoke(type, message, options, server);
                return next.Invoke(type, message, options, server);
            }
            return sender;
        });
        return manager;
    }
    /// <summary>
    /// 使用【发送消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, Func<SendDelegate, SendDelegate> middleware)
        => manager.Use(name: null, middleware);
    #endregion

    #region 接收消息 中间件
    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, IReceiveMiddleware middleware)
        => Use(manager, name: null, middleware);
    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="name"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, string? name, IReceiveMiddleware middleware)
    {
        ThrowIfNull(middleware);
        manager.Use(name, next =>
        {
            Task<bool> receiver(MessageType type, MessageDescriptor message, IReceiveOptions options, IServerOptions server)
                => middleware.Receive(type, message, options, server, next);
            return receiver;
        });
        return manager;
    }
    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, Action<MessageType, MessageDescriptor, IReceiveOptions, IServerOptions> middleware)
        => Use(manager, name: null, middleware);
    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="name">中间件名称</param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, string? name, Action<MessageType, MessageDescriptor, IReceiveOptions, IServerOptions> middleware)
    {
        ThrowIfNull(middleware);
        manager.Use(name, next =>
        {
            Task<bool> receiver(MessageType type, MessageDescriptor message, IReceiveOptions options, IServerOptions server)
            {
                middleware.Invoke(type, message, options, server);
                return next.Invoke(type, message, options, server);
            }
            return receiver;
        });
        return manager;
    }
    /// <summary>
    /// 使用【接收消息】中间件
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="middleware"></param>
    /// <returns></returns>
    public static IMessageManager Use(this IMessageManager manager, Func<ReceiveDelegate, ReceiveDelegate> middleware)
        => manager.Use(name: null, middleware);
    #endregion

    #endregion
}
