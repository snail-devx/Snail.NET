using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Message;
using Snail.Abstractions.Message.Delegates;

namespace Snail.Message
{
    /// <summary>
    /// 消息管理器
    /// </summary>
    [Component<IMessageManager>(Lifetime = LifetimeType.Singleton)]
    public sealed class MessageManager : IMessageManager
    {
        #region 属性变量
        /// <summary>
        /// 发送消息的中间件代理
        /// </summary>
        private readonly IMiddlewareProxy<SendDelegate> _sendMiddlewares;
        /// <summary>
        /// 接收消息的中间件代理
        /// </summary>
        private readonly IMiddlewareProxy<ReceiveDelegate> _receiveMiddlewares;
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public MessageManager()
        {
            //  中间件代理；并做一些中间件的初始化工作
            _sendMiddlewares = new MiddlewareProxy<SendDelegate>();
            _sendMiddlewares.Use(name: MIDDLEWARE_ShareKeyChain, middleware: null)
                            .Use(name: MIDDLEWARE_Logging, middleware: null);
            _receiveMiddlewares = new MiddlewareProxy<ReceiveDelegate>();
            _receiveMiddlewares.Use(name: MIDDLEWARE_Logging, middleware: null)
                               .Use(name: MIDDLEWARE_RunContext, middleware: null)
                               .Use(name: MIDDLEWARE_Logging, middleware: null);
        }
        #endregion

        #region IMessageManager
        /// <summary>
        /// 使用【发送消息】中间件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="middleware"></param>
        /// <returns></returns>
        IMessageManager IMessageManager.Use(string? name, Func<SendDelegate, SendDelegate> middleware)
        {
            _sendMiddlewares.Use(name, middleware);
            return this;
        }
        /// <summary>
        /// 构建【发送消息】中间件执行委托
        /// </summary>
        /// <param name="start">入口委托；所有中间件都执行了，在执行此委托处理实际业务逻辑</param>
        /// <returns>执行委托</returns>
        SendDelegate IMessageManager.Build(SendDelegate start)
            => _sendMiddlewares.Build(start, onionMode: true);

        /// <summary>
        /// 使用【接收消息】中间件
        /// </summary>
        /// <param name="name">中间件名称；传入确切值，则会先查找同名中间件是否存在，若存在则替换到原先为止；否则始终追加</param>
        /// <param name="middleware">中间件</param>
        /// <returns>消息管理器自身，方便链式调用</returns>
        IMessageManager IMessageManager.Use(string? name, Func<ReceiveDelegate, ReceiveDelegate> middleware)
        {
            _receiveMiddlewares.Use(name, middleware);
            return this;
        }
        /// <summary>
        /// 构建【接收消息】中间件执行委托
        /// </summary>
        /// <param name="start">入口委托；所有中间件都执行了，在执行此委托处理实际业务逻辑</param>
        /// <returns>执行委托</returns>
        ReceiveDelegate IMessageManager.Build(ReceiveDelegate start)
            => _receiveMiddlewares.Build(start, onionMode: true);
        #endregion
    }
}
