namespace Snail.Abstractions.Web.Interfaces
{
    /// <summary>
    /// HTTP请求提供程序；负责进行实际HTTP请求发送
    /// </summary>
    public interface IHttpProvider
    {
        /// <summary>
        /// 发送HTTP请求
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <param name="server">服务器配置选项</param>
        /// <returns></returns>
        Task<HttpResponseMessage> Send(HttpRequestMessage request, IServerOptions server);
    }
}
