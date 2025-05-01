using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.Extensions
{
    /// <summary>
    /// <see cref="IHttpManager"/>扩展方法
    /// </summary>
    public static class HttpManagerExtensions
    {
        #region 公共方法

        #region use：使用中间件
        /// <summary>
        /// 使用中间件：name为null
        /// </summary>
        /// <param name="manager">Http管理器实例</param>
        /// <param name="middleware">中间件</param>
        /// <returns><paramref name="manager"/>，链式调用</returns>
        public static IHttpManager Use(this IHttpManager manager, IHttpMiddleware middleware)
           => Use(manager, name: null, middleware);
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="manager">Http管理器实例</param>
        /// <param name="name">中间件名称</param>
        /// <param name="middleware">中间件</param>
        /// <returns><paramref name="manager"/>，链式调用</returns>
        public static IHttpManager Use(this IHttpManager manager, string? name, IHttpMiddleware middleware)
        {
            ThrowIfNull(middleware);
            manager.Use(name, next =>
            {
                HttpDelegate @delegate = (HttpRequestMessage request, IServerOptions server) =>
                    middleware.SendAsync(request, server, next);
                return @delegate;
            });
            return manager;
        }

        /// <summary>
        /// 使用中间件：name为null
        /// </summary>
        /// <param name="manager">Http管理器实例</param>
        /// <param name="middleware">中间件</param>
        /// <returns><paramref name="manager"/>，链式调用</returns>
        public static IHttpManager Use(this IHttpManager manager, Action<HttpRequestMessage, IServerOptions> middleware)
             => Use(manager, name: null, middleware);
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="manager">Http管理器实例</param>
        /// <param name="name">中间件名称</param>
        /// <param name="middleware">中间件</param>
        /// <returns><paramref name="manager"/>，链式调用</returns>
        public static IHttpManager Use(this IHttpManager manager, string? name, Action<HttpRequestMessage, IServerOptions> middleware)
        {
            ThrowIfNull(middleware);
            manager.Use(name, next =>
            {
                HttpDelegate @delegate = (HttpRequestMessage request, IServerOptions server) =>
                {
                    middleware.Invoke(request, server);
                    return next.Invoke(request, server);
                };
                return @delegate;
            });
            return manager;
        }

        /// <summary>
        /// 使用中间件：name为null
        /// </summary>
        /// <param name="manager">Http管理器实例</param>
        /// <param name="middleware">中间件</param>
        /// <returns><paramref name="manager"/>，链式调用</returns>
        public static IHttpManager Use(this IHttpManager manager, Func<HttpDelegate, HttpDelegate> middleware)
        {
            manager.Use(name: null, middleware);
            return manager;
        }
        #endregion

        #region use：内置中间件
        /** 不对外提供服务，外部可以直接使用<see cref="MIDDLEWARE_Logging"/>和<see cref="MIDDLEWARE_ShareKeyChain"/>使用具名中间件
        /// <summary>
        /// 使用【共享钥匙串】中间件
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public static IHttpManager UseShareKeyChain(this IHttpManager manager, IHttpMiddleware middleware)
            => Use(manager, name: MIDDLEWARE_ShareKeyChain, middleware);
        /// <summary>
        /// 使用【日志】中间件
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public static IHttpManager UseLogging(this IHttpManager manager, IHttpMiddleware middleware)
            => Use(manager, name: MIDDLEWARE_Logging, middleware);
        */
        #endregion

        #endregion
    }
}
