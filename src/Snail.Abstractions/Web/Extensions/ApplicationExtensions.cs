using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.Extensions
{
    /// <summary>
    /// <see cref="IApplication"/>针对<see cref="Web"/>下的相关扩展
    /// </summary>
    public static class ApplicationExtensions
    {
        #region 公共方法

        #region IHttpManager 扩展
        /// <summary>
        /// 添加HTTP请求服务
        /// </summary>
        /// <param name="app"></param>
        /// <param name="useLogging">是否启用【日志】中间件</param>
        /// <param name="useShareKeyChain">是否启用【共享钥匙串】中间件</param>
        /// <returns></returns>
        public static IApplication AddHttpService(this IApplication app, bool useLogging = true, bool useShareKeyChain = true)
        {
            //  对HTTP请求管理器做一下预热操作
            app.OnRegister += () =>
            {
                IHttpManager http = app.ResolveRequired<IHttpManager>();
                if (useLogging == true)
                {
                    IHttpMiddleware middleware = app.ResolveRequired<IHttpMiddleware>(key: MIDDLEWARE_Logging);
                    http.Use(name: MIDDLEWARE_Logging, middleware);
                }
                if (useShareKeyChain == true)
                {
                    IHttpMiddleware? middleware = app.ResolveRequired<IHttpMiddleware>(key: MIDDLEWARE_ShareKeyChain);
                    http.Use(name: MIDDLEWARE_ShareKeyChain, middleware);
                }
            };

            return app;
        }
        #endregion

        #endregion
    }
}
