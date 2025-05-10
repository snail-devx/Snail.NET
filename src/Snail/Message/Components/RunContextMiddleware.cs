using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Message.Components
{
    /// <summary>
    /// 【运行时上下文】中间件
    /// </summary>
    [Component<IMessageMiddleware>(Key = MIDDLEWARE_RunContext, Lifetime = LifetimeType.Singleton)]
    public sealed class RunContextMiddleware : IMessageMiddleware
    {
        #region 公共方法
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
            //  发送消息时，不做任何处理
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
            //  启用全新的RunContext对象
            RunContext context = RunContext.New();
            //  一些特定的长下文信息初始化过来
            string? tmpString = null;
            //      父级操作Id
            message.Context?.Remove(CONTEXT_ParentActionId, out tmpString);
            if (string.IsNullOrEmpty(tmpString) == false)
            {
                context.Add(CONTEXT_ParentActionId, tmpString);
            }
            //  进入下一个操作
            return next(type, message, options, server);
        }
        #endregion
    }
}
