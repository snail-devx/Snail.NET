using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.Delegates;

/// <summary>
/// HTTP请求委托
/// </summary>
/// <param name="request">请求对象</param>
/// <param name="server">请求服务器地址配置</param>
/// <returns></returns>
public delegate Task<HttpResponseMessage> HttpDelegate(HttpRequestMessage request, IServerOptions server);
