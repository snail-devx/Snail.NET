using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Message.Components;

/// <summary>
/// 【运行时上下文】中间件
/// </summary>
[Component<IMessageMiddleware>(Key = MIDDLEWARE_RunContext)]
public class RunContextMiddleware : IMessageMiddleware
{
    #region IMessageMiddleware
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="message">发送的消息数据</param>        
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <param name="server">消息服务器地址消息发送哪里</param>
    /// <param name="next">下一个消息处理委托</param>
    /// <returns></returns>
    Task<bool> ISendMiddleware.Send(MessageType type, MessageData message, IMessageOptions options, IServerOptions server, SendDelegate next)
    {
        InitializeSend(message, RunContext.Current);
        return next(type, message, options, server);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <param name="type">消息类型：如mq、pubsub</param>
    /// <param name="message">发送的消息数据</param>        
    /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
    /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
    /// <param name="next">下一个消息处理委托</param>
    /// <returns></returns>
    Task<bool> IReceiveMiddleware.Receive(MessageType type, MessageData message, IReceiveOptions options, IServerOptions server, ReceiveDelegate next)
    {
        /* 启用全新的RunContext对象 */
        RunContext context = RunContext.New();
        InitializeReceive(message, context);

        return next(type, message, options, server);
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 【发送消息】初始化上下文信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="context"></param>
    protected virtual void InitializeSend(MessageData message, RunContext context)
    {
        //  目前不做任何操作，后期考虑把上下文上的共享信息写入message中，传递到下一个请求中进行共享
    }
    /// <summary>
    /// 【接收消息】初始化上下文信息
    /// </summary>
    /// <param name="message"></param>
    /// <param name="context"></param>
    protected virtual void InitializeReceive(MessageData message, RunContext context)
    {
        //  目前不做任何操作，后期考虑从message中获取共享数据写入运行时上下文
    }
    #endregion

}
