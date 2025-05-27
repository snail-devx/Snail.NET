using System.Net.Http.Headers;
using Snail.Abstractions.Web.DataModels;
using Snail.Utilities.Common.Extensions;

namespace Snail.Abstractions.Web.Extensions
{
    /// <summary>
    /// <see cref="IHttpRequestor"/>扩展方法
    /// </summary>
    public static class HttpRequestorExtensions
    {
        #region 公共方法

        #region 请求发送：暂时不对外提供同步方法，都走Task异步
        /// <summary>
        /// 发送Get请求
        /// </summary>
        /// <param name="requestor">HTTP请求器</param>
        /// <param name="url">请求url地址</param>
        /// <returns></returns>
        /// <remarks>暂时不支持delete请求提交数据</remarks>
        public static Task<HttpResult> Get(this IHttpRequestor requestor, string url)
            => requestor.Send(new HttpRequestMessage(HttpMethod.Get, url));

        /// <summary>
        /// 发送Post请求
        /// </summary>
        /// <typeparam name="T">提交数据的类型</typeparam>
        /// <param name="requestor">HTTP请求器</param>
        /// <param name="url">请求url地址</param>
        /// <param name="data">提交数据</param>
        /// <returns></returns>
        public static Task<HttpResult> Post<T>(this IHttpRequestor requestor, string url, T? data = default)
            => requestor.Send(BuildRequestByData(HttpMethod.Post, url, data));

        /// <summary>
        /// 发送Put请求
        /// </summary>
        /// <typeparam name="T">提交数据的类型</typeparam>
        /// <param name="requestor">HTTP请求器</param>
        /// <param name="url">请求url地址</param>
        /// <param name="data">提交数据</param>
        /// <returns></returns>
        public static Task<HttpResult> Put<T>(this IHttpRequestor requestor, string url, T? data = default)
            => requestor.Send(BuildRequestByData(HttpMethod.Put, url, data));

        /// <summary>
        /// 发送Delete请求
        /// </summary>
        /// <param name="requestor">HTTP请求器</param>
        /// <param name="url">请求url地址</param>
        /// <returns></returns>
        /// <remarks>暂时不支持delete请求提交数据</remarks>
        public static Task<HttpResult> Delete(this IHttpRequestor requestor, string url)
            => requestor.Send(new HttpRequestMessage(HttpMethod.Delete, url));
        #endregion

        #endregion

        #region 私有方法
        /// <summary>
        /// 构建提交数据的HTTP请求
        /// </summary>
        /// <param name="method"></param>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static HttpRequestMessage BuildRequestByData<T>(HttpMethod method, string? url, T? data)
        {
            //  构建请求对象
            method ??= HttpMethod.Post;
            HttpRequestMessage request = new(method, url);
            //  组装提交数据；非HttpContent时强制json格式
            if (data != null)
            {
                if (data is HttpContent content == false)
                {
                    string str = data.AsJson();
                    content = new StringContent(str);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
                request.Content = content;
            }
            return request;
        }
        #endregion
    }
}
