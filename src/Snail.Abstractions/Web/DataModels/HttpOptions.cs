
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web.DataModels;
/// <summary>
/// HTTP请求配置选项
/// <para>做默认实现</para>
/// </summary>
public class HttpOptions : IHttpOptions
{
    #region IHttpOptions
    /// <summary>
    /// 操作在何时视为已完成
    /// <para>1、流式响应时，赋值为：<see cref="HttpCompletionOption.ResponseHeadersRead"/></para>
    /// <para>2、其他情况忽略即可</para>
    /// </summary>
    public HttpCompletionOption? CompletionOption { init; get; }
    /// <summary>
    /// 取消令牌：用于判断HTTP请求是否停止了
    /// </summary>
    public CancellationToken? Cancellation { init; get; }

    /// <summary>
    /// 解析配置选项
    /// <para>满足一些特定配置选项扩展逻辑，如干预中间件逻辑等</para>
    /// </summary>
    /// <typeparam name="T">配置选项类型</typeparam>
    /// <param name="request">http请求对象</param>
    /// <returns>解析出来的配置选项，不存在则返回null</returns>
    object? IHttpOptions.Resolve<T>(HttpRequestMessage request)
    {
        return null;
    }
    #endregion
}