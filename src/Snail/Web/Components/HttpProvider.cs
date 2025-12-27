using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Web.Components;

/// <summary>
/// HTTP请求提供程序；负责进行实际HTTP请求发送
/// </summary>
[Component<IHttpProvider>]
public sealed class HttpProvider : IHttpProvider
{
    #region 属性变量
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
    Task<HttpResponseMessage> IHttpProvider.Send(HttpRequestMessage request, IServerOptions server)
    {
        ThrowIfNull(request);
        ServerDescriptor? descriptor = _manager.GetServer(server);
        ThrowIfNull(descriptor, $"取到的服务器信息为null:${server}");
        Uri baseAddress = new(descriptor!.Server);
        return HttpProxy.Send(baseAddress, request);
    }
    #endregion
}
