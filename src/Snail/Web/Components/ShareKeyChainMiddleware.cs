using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Common.Extensions;

namespace Snail.Web.Components
{
    /// <summary>
    /// HTTP请求的【共享钥匙串】中间件
    /// </summary>
    [Component<IHttpMiddleware>(Lifetime = LifetimeType.Singleton, Key = MIDDLEWARE_ShareKeyChain)]
    public class ShareKeychainMiddleware : IHttpMiddleware
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
            string? shareKeyChain = RunContext.Current.GetShareKeyChain()?.AsJson();
            if (string.IsNullOrEmpty(shareKeyChain) == false)
            {
                //  这里其实应该做个编码，但是net46那块编码接收数据后未自动解码；这里先不编码，反正共享钥匙串传递过去的数据也不会出现“;”这类关键字
                //shareKeyChain = StringHelper.GetUrlEncode(shareKeyChain);
                request.Headers.Add("Cookie", $"{CONTEXT_ShareKeyChain}={shareKeyChain}");
            }
            return next(request, server);
        }
        #endregion
    }
}
