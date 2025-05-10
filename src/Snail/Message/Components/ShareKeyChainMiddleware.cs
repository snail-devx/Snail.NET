using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Common.Extensions;

namespace Snail.Message.Components
{
    /// <summary>
    /// 共享钥匙串 中间件
    /// </summary>
    [Component<IMessageMiddleware>(Key = MIDDLEWARE_ShareKeyChain, Lifetime = LifetimeType.Singleton)]
    public class ShareKeyChainMiddleware : IMessageMiddleware
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
            //  钥匙串非null才操作Context对象
            string? keyChain = RunContext.Current.GetShareKeyChain()?.AsJson();
            if (string.IsNullOrEmpty(keyChain) == false)
            {
                //  若果上下文Context为null，则做上标记
                message.Context ??= new Dictionary<string, string>
                {
                    [CONTEXT_ContextIsNull] = "1"
                };
                //  加上ShareKeyChain
                message.Context[CONTEXT_ShareKeyChain] = keyChain;
            }
            //  进入下一个操作
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
            //  将共享钥匙串的相关信息初始化到运行时上下文中
            RunContext context = RunContext.Current;
            if (message.Context != null)
            {
                //  初始化【共享钥匙串】信息
                message.Context.Remove(CONTEXT_ShareKeyChain, out string? tmpString);
                context.InitShareKeyChain(tmpString);
            }
            //  进入下一个操作
            return next(type, message, options, server);
        }
        #endregion

    }
}