using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Web.Delegates;

namespace Snail.Abstractions.Web
{
    /// <summary>
    /// 接口约束：HTTP管理器，负责管理HTTP服务器、HTTP中间件管理 <br />
    ///     1、实现<see cref="IServerManager"/>进行服务器管理 <br />
    ///     2、实现<see cref="IMiddlewareProxy{HttpDelegate}"/>实现中间件管理；支持中间件干涉http请求发送 <br />
    /// </summary>
    public interface IHttpManager : IServerManager, IMiddlewareProxy<HttpDelegate>
    {
    }
}
