using Snail.Abstractions.Web.Delegates;

namespace Snail.Abstractions.Web.Interfaces
{
    /// <summary>
    /// 接口约束：HTTP请求中间件
    /// </summary>
    public interface IHttpMiddleware
    {
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="request">http请求</param>
        /// <param name="server">请求服务器</param>
        /// <param name="next">下一个操作</param>
        /// <returns></returns>
        Task<HttpResponseMessage> Send(HttpRequestMessage request, IServerOptions server, HttpDelegate next);
    }
}
