using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web
{
    /// <summary>
    /// HTTP请求器；负责发送HTTP请求到具体的<see cref="IHttpProvider"/>
    /// </summary>
    public interface IHttpRequestor
    {
        /// <summary>
        /// 发送请求；异步可等待
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <returns>返回结果</returns>
        Task<HttpResult> Send(HttpRequestMessage request);
    }
}
