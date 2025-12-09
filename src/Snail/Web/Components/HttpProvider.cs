using Snail.Abstractions.Common.DataModels;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Web.Extensions;
using System.Net;

namespace Snail.Web.Components;

/// <summary>
/// HTTP请求提供程序；负责进行实际HTTP请求发送
/// </summary>
[Component<IHttpProvider>]
public sealed class HttpProvider : IHttpProvider
{
    #region 属性变量
    /// <summary>
    /// <see cref="HttpClient"/>对象池，每个链接使用超过2小时就创建新的，解决dns域名修改时无法实时更新的问题
    /// </summary>
    private static readonly ObjectPool<PoolObject<HttpClient>> _clientPool = new(TimeSpan.FromHours(2));
    /// <summary>
    /// 默认的httpclient句柄配置
    /// </summary>
    private static readonly HttpClientHandler _defaultHttpClienHandler = new HttpClientHandler()
    {
        UseCookies = false,
        //  忽略服务器端自定义证书；后期进行正式绑定 https://stackoverflow.com/questions/69395823/the-ssl-connection-could-not-be-established-system-security-authentication-auth
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    /// <summary>
    /// Http服务器地址
    /// </summary>
    private readonly IHttpManager _manager;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="manager">HTTP管理器</param>
    [Inject]
    public HttpProvider(IHttpManager manager)
    {
        _manager = ThrowIfNull(manager);
    }
    #endregion

    #region IHttpProvider
    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    /// <param name="request">请求对象</param>
    /// <param name="server">服务器配置选项</param>
    /// <returns></returns>
    async Task<HttpResponseMessage> IHttpProvider.Send(HttpRequestMessage request, IServerOptions server)
    {
        ThrowIfNull(request);
        ServerDescriptor? descriptor = _manager.GetServer(server);
        ThrowIfNull(descriptor, $"取到的服务器信息为null:${server}");
        Uri baseAddress = new Uri(descriptor!.Server);
        HttpResponseMessage response = await Send(baseAddress, request);
        return response;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    /// <param name="baseAddress">目标服务器地址</param>
    /// <param name="request">请求对象</param>
    /// <returns></returns>
    public static Task<HttpResponseMessage> Send(Uri baseAddress, in HttpRequestMessage request)
    {
        ThrowIfNull(baseAddress);
        ThrowIfNull(request);
        //  对uri进行校验；提取UserInfo信息做Authorization验证；后期考虑缓存，空间换时间
        baseAddress = baseAddress.TryClearUserInfo(out string userInfo);
        //  构建hc对象：不用设置为using状态，默认都是空闲状态，确保同一服务器始终一个链接；使用超过2小时，ObjectPool会自动回收创建新链接
        PoolObject<HttpClient> proxy = _clientPool.GetOrAdd(
             predicate: proxy => proxy.Object.BaseAddress == baseAddress,
             addFunc: () => new PoolObject<HttpClient>(new HttpClient(_defaultHttpClienHandler)
             {
                 BaseAddress = baseAddress,
                 Timeout = TimeSpan.FromMinutes(10),/*10分钟超时时间*/
             }),
             autoUsing: false
         );
        ThrowIfNull(proxy?.Object, $"ObjectPool<PoolObject<HttpClient>>返回null；无法发送HTTP请求。URI：{baseAddress}");
        //  针对cookie做处理：Header.Add("Cookie")设置Cookie，最终会以“,”分割传递，而不是期望的“;”
        if (request.Headers.TryGetValues("Cookie", out IEnumerable<string>? values) == true)
        {
            string cookieValue = string.Join(';', values);
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", cookieValue);
        }
        //  添加Authorization验证：默认basic验证；后期再扩展
        if (userInfo != null)
        {
            userInfo = userInfo.AsBase64Encode();
            request.Headers.Add("Authorization", $"basic {userInfo}");
        }
        //  针对其他系统的兼容：特定兼容，一些优化逻辑
        {
#pragma warning disable SYSLIB0014
            //      取消100-continue请求，为true时会发送post请求前会先发送包含的【Expect:100-continue】头部请求，询问服务器是否愿意接受接收数据
            //      服务器java项目，且spring cloud gateway 网关，在post的情况下，如果不在此属性始终报504的错误
            ServicePointManager.Expect100Continue = false;
#pragma warning restore SYSLIB0014
        }
        //  发送请求
        return proxy!.Object.SendAsync(request);
    }
    #endregion
}
