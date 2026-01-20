using Snail.Utilities.Web.Extensions;
using System.Net;

namespace Snail.Web.Components;
/// <summary>
/// HTTP 代理类
/// <para>1、代理<see cref="HttpClient"/>实现hc复用</para>
/// <para>2、自动管理回收空闲hc</para>
/// </summary>
public sealed class HttpProxy : PoolableObject<HttpClient>, IPoolable
{
    #region 属性变量
    /// <summary>
    /// <see cref="HttpClient"/>对象池，每个链接使用超过2小时就创建新的，解决dns域名修改时无法实时更新的问题
    /// </summary>
    private static readonly ObjectPool<HttpProxy> _clientPool = new(FromHours(2));
    /// <summary>
    /// 默认的httpclient句柄配置
    /// </summary>
    private static readonly HttpClientHandler _defaultHttpClienHandler = new()
    {
        UseCookies = false,
        //  忽略服务器端自定义证书；后期进行正式绑定 https://stackoverflow.com/questions/69395823/the-ssl-connection-could-not-be-established-system-security-authentication-auth
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    /// <summary>
    /// 使用计数
    /// </summary>
    private int _usingCount = 0;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    private HttpProxy(HttpClient hc) : base(hc)
    { }
    #endregion

    #region IPoolable
    /// <summary>
    /// 是否处于闲置状态
    /// </summary>
    bool IPoolable.IsIdle => _usingCount == 0;
    /// <summary>
    /// 使用对象
    /// </summary>
    /// <returns></returns>
    IPoolable IPoolable.Using()
    {
        //  重写实现接口，但什么都不做，主要避免外部做改变， 是否闲置，由使用计数来做
        return this;
    }
    /// <summary>
    /// 对象使用完了
    /// </summary>
    void IPoolable.Used()
    {
        //  重写实现接口，但什么都不做，主要避免外部做改变， 是否闲置，由使用计数来做
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 发送HTTP请求
    /// </summary>
    /// <param name="baseAddress"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public static Task<HttpResponseMessage> Send(Uri baseAddress, in HttpRequestMessage request)
    {
        ThrowIfNull(baseAddress);
        ThrowIfNull(request);
        //  对uri进行校验，提取UserInfo信息做Authorization验证；后期考虑缓存，空间换时间
        baseAddress = baseAddress.TryClearUserInfo(out string userInfo);
        if (userInfo != null)
        {
            userInfo = userInfo.AsBase64Encode();
            request.Headers.Add("Authorization", $"basic {userInfo}");
        }
        /** 构建hc对象：不用设置为using状态，默认都是空闲状态，确保同一服务器始终一个链接；使用超过2小时，ObjectPool会自动回收创建新链接
         *  1、构建hc对象自动缓存，下次访问时，会自动使用缓存的hc对象
         *  2、构建的hc对象，不进行using，new HttpProxy后，默认空闲时间为当前时间，从而实现闲置时间超过2小时后自动销毁回收
         *  3、复用hc规则：
         *      1、服务器地址相同时，复用同一个链接；服务器地址不同时，创建新的链接；
         *      2、命中复用hc后，判断hc的空间时间需 小于 <see cref="ObjectPool{T}.IdleInterval"/> 时才复用；避免刚好满足销毁条件时，又复用，从而复用了已销毁的对象
         */
        HttpProxy proxy = _clientPool.GetOrAdd
        (
            predicate: py => py.Object.BaseAddress == baseAddress && DateTime.UtcNow.Subtract(py.IdleTime) < _clientPool.IdleInterval,
            addFunc: () =>
            {
                // 构建HttpClient时，传入固定handler，不用每次创建，但需要强制指定disposeHandler=false（hc dispose时不销毁handle），否则handler无法复用
                HttpClient hc = new HttpClient(_defaultHttpClienHandler, disposeHandler: false)
                {
                    BaseAddress = baseAddress,
                    Timeout = FromMinutes(10),/*10分钟超时时间*/
                };
                return new HttpProxy(hc);
            },
            autoUsing: false
        );
        //ThrowIfNull(proxy, $"ObjectPool<HttpClientProxy>返回null；无法发送HTTP请求。URI：{baseAddress}");
        //  发送HTTP请求，进行请求计数处理
        Interlocked.Increment(ref proxy._usingCount);
        try
        {
            //  针对cookie做处理：Header.Add("Cookie")设置Cookie，最终会以“,”分割传递，而不是期望的“;”
            if (request.Headers.TryGetValues("Cookie", out IEnumerable<string>? values) == true)
            {
                string cookieValue = string.Join(';', values);
                request.Headers.Remove("Cookie");
                request.Headers.Add("Cookie", cookieValue);
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
            return proxy.Object.SendAsync(request);
        }
        finally
        {
            Interlocked.Decrement(ref proxy._usingCount);
        }
    }
    #endregion
}