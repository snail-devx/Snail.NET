using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Web.Components;

/// <summary>
/// 【运行时上下文】中间件
/// </summary>
[Component<IHttpMiddleware>(Key = MIDDLEWARE_RunContext)]
public class RunContextMiddleware : IHttpMiddleware
{
    #region IHttpMiddleware
    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="request">http请求</param>
    /// <param name="server">请求服务器</param>
    /// <param name="next">下一个操作</param>
    /// <returns></returns>
    Task<HttpResponseMessage> IHttpMiddleware.Send(HttpRequestMessage request, IServerOptions server, HttpDelegate next)
    {
        Initialize(request, RunContext.Current);
        return next(request, server);
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 基于【运行时上下文】初始化http请求
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    protected virtual void Initialize(in HttpRequestMessage request, in RunContext context)
    {
        //  目前不做任何操作，后期考虑把上下文上的共享信息写入cookie中，传递到下一个请求中进行共享
    }
    #endregion
}