using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.Delegates;

namespace Snail.Web
{
    /// <summary>
    ///HTTP管理器，负责管理HTTP服务器、HTTP中间件管理 <br />
    ///     1、实现<see cref="IServerManager"/>进行服务器管理；读取应用程序配置下直属工作空间的server资源初始化服务器 <br />
    ///         继承<see cref="ServerManager"/>，自动实现<see cref="IServerManager"/>接口所有功能<br />
    ///     2、实现<see cref="IMiddlewareProxy{HttpDelegate}"/>实现中间件管理；支持中间件干涉http请求发送 <br />
    ///         内部构建使用<see cref="MiddlewareProxy{HttpDelegate}"/>中转实现
    /// </summary>
    /// <remarks>无法多继承，否则可直接</remarks>
    [Component<IHttpManager>(Lifetime = LifetimeType.Singleton)]
    public sealed class HttpManager : ServerManager, IHttpManager
    {
        #region 属性变量
        /// <summary>
        /// 中间件代理
        /// </summary>
        private readonly IMiddlewareProxy<HttpDelegate> _proxy;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        public HttpManager(IApplication app)
            : base(app, rsCode: "server")
        {
            ThrowIfNull(app);
            _proxy = app.ResolveRequired<IMiddlewareProxy<HttpDelegate>>();
            app.Resolve<IInitializer<IHttpManager>>()?.Initialize(this);
        }
        #endregion

        #region IHttpManager
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="name">中间件名称；传入确切值，则会先查找同名中间件是否存在，若存在则替换到原先为止；否则始终追加</param>
        /// <param name="middleware">中间件委托；为null表示占位，此时<paramref name="name"/>不能为null</param>
        /// <returns>代理器自身，方便立案时调用</returns>
        IMiddlewareProxy<HttpDelegate> IMiddlewareProxy<HttpDelegate>.Use(in string? name, in Func<HttpDelegate, HttpDelegate>? middleware)
            => _proxy.Use(name, middleware);
        /// <summary>
        /// 构建中间件执行委托
        /// </summary>
        /// <param name="start">入口委托；所有中间件都执行了，再执行此委托处理实际业务逻辑</param>
        /// <param name="onionMode">洋葱模式，越早use的中间件越早执行；否则越晚use的中间件越早执行</param>
        /// <returns>中间件执行委托</returns>
        HttpDelegate IMiddlewareProxy<HttpDelegate>.Build(in HttpDelegate start, in bool onionMode)
            => _proxy.Build(start, onionMode);
        #endregion
    }
}
