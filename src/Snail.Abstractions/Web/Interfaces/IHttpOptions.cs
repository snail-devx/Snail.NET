namespace Snail.Abstractions.Web.Interfaces;

/// <summary>
/// 接口约束：HTTP请求相关配置选项
/// <para>1、配合<see cref="IHttpRequestor.Send"/>使用，进行最细力度的请求发送控制</para>
/// <para>2、基于配置，用于干预中间件运行逻辑</para>
/// </summary>
public interface IHttpOptions
{
    /// <summary>
    /// 解析配置选项
    /// </summary>
    /// <typeparam name="T">配置选项类型</typeparam>
    /// <param name="request">http请求对象</param>
    /// <returns>解析出来的配置选项，不存在则返回null</returns>
    object? Resolve<T>(HttpRequestMessage request);
}
