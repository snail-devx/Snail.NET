using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.Extensions;

/// <summary>
/// <see cref="IHttpManager"/>扩展方法
/// </summary>
public static class HttpManagerExtensions
{
    extension(IHttpManager manager)
    {
        #region 公共方法

        #region use：使用中间件
        /// <summary>
        /// 使用中间件：name为null
        /// </summary>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public IHttpManager Use(IHttpMiddleware middleware)
           => Use(manager, name: null, middleware);
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="name">中间件名称</param>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public IHttpManager Use(string? name, IHttpMiddleware middleware)
        {
            ThrowIfNull(middleware);
            manager.Use(name, next =>
            {
                HttpDelegate @delegate = (HttpRequestMessage request, IServerOptions server) =>
                    middleware.Send(request, server, next);
                return @delegate;
            });
            return manager;
        }

        /// <summary>
        /// 使用中间件：name为null
        /// </summary>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public IHttpManager Use(Action<HttpRequestMessage, IServerOptions> middleware)
             => Use(manager, name: null, middleware);
        /// <summary>
        /// 使用中间件
        /// </summary>
        /// <param name="name">中间件名称</param>
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public IHttpManager Use(string? name, Action<HttpRequestMessage, IServerOptions> middleware)
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
        /// <param name="middleware">中间件</param>
        /// <returns></returns>
        public IHttpManager Use(Func<HttpDelegate, HttpDelegate> middleware)
        {
            manager.Use(name: null, middleware);
            return manager;
        }
        #endregion

        #region use：内置中间件

        #endregion

        #endregion
    }
}
