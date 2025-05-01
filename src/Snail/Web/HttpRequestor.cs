using System.Net.Http.Headers;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Web
{
    /// <summary>
    /// HTTP请求器；负责发送HTTP请求到具体的<see cref="IHttpProvider"/>
    /// </summary>
    [Component<IHttpRequestor>(Lifetime = LifetimeType.Transient)]
    public sealed class HttpRequestor : IHttpRequestor
    {
        #region 属性变量
        /// <summary>
        /// 实际的http请求发送器
        /// </summary>
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _sender;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app">应用程序实例</param>
        /// <param name="server">服务器配置选项；非null，请求发往哪台服务器</param>
        /// <param name="provider">HTTP提供程序；为null则使用默认的</param>
        /// <remarks>作为【依赖注入】的注入入口</remarks>
        [Inject]
        public HttpRequestor(IApplication app, IServerOptions server, IHttpProvider? provider = null)
        {
            ThrowIfNull(app);
            ThrowIfNull(server);
            //  预热依赖对象
            IHttpManager manager = app.ResolveRequired<IHttpManager>();
            provider = provider ?? app.ResolveRequired<IHttpProvider>();
            //  构建中间件，构建http发送委托
            HttpDelegate middleware = manager.Build(provider.Send, onionMode: true);
            _sender = request => middleware.Invoke(request, server);
        }
        #endregion

        #region IHttpRequestor
        /// <summary>
        /// 发送请求；异步可等待
        /// </summary>
        /// <param name="request">请求对象</param>
        /// <returns>返回结果</returns>
        async Task<HttpResult> IHttpRequestor.Send(HttpRequestMessage request)
        {
            ThrowIfNull(request);
            ThrowIfNull(request.Method);
            ThrowIfNull(request.RequestUri);
            //  发送请求；默认接收json格式
            if (request.Headers.Accept.Count == 0)
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            HttpResponseMessage response = await _sender.Invoke(request);
            //  构建请求结果：后期考虑做一下异常拦截，把异常信息具象化，如是否请求成功，请求状态码，异常信息都解析出来
            return new HttpResult(response);
        }
        #endregion
    }
}
