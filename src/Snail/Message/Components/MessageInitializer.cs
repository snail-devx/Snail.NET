using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Message;
using Snail.Abstractions.Message.Extensions;
using Snail.Abstractions.Message.Interfaces;

namespace Snail.Message.Components
{
    /// <summary>
    /// 消息初始化器；用于进行<see cref="IMessageManager"/>实例初始化
    /// </summary>
    [Component<IInitializer<IMessageManager>>(Lifetime = LifetimeType.Transient)]
    public class MessageInitializer : IInitializer<IMessageManager>
    {
        #region 属性变量
        /// <summary>
        /// 应用程序实例
        /// </summary>
        protected readonly IApplication App;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public MessageInitializer(IApplication app)
        {
            App = ThrowIfNull(app);
        }
        #endregion

        #region IInitializer<IMessageManager>
        /// <summary>
        /// 初始化消息
        /// </summary>
        /// <param name="manager">消息管理器</param>
        void IInitializer<IMessageManager>.Initialize(IMessageManager manager)
        {
            //  配置日志和运行上下文中间件
            manager.Use(MIDDLEWARE_RunContext, App.ResolveRequired<IMessageMiddleware>(key: MIDDLEWARE_RunContext))
                   .Use(MIDDLEWARE_Telemetry, App.ResolveRequired<IMessageMiddleware>(key: MIDDLEWARE_Telemetry));
        }
        #endregion
    }
}