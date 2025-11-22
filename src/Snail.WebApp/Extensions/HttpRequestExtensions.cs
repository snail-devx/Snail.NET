namespace Snail.WebApp.Extensions;

/// <summary>
/// HttpRequest扩展方法
/// </summary>
public static class HttpRequestExtensions
{
    #region 公共方法
    /// <summary>
    /// 获取请求的URL地址，绝对路径，但不包含锚点值
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string AbsoluteUrl(this HttpRequest request)
    {
        var absoluteUri = string.Concat(
                request.Scheme,
                "://",
                request.Host.ToUriComponent(),
                request.PathBase.ToUriComponent(),
                request.Path.ToUriComponent(),
                request.QueryString.ToUriComponent()
        );
        return absoluteUri;
    }

    /// <summary>
    /// 获取请求的 UserAgent 值
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string? UserAgent(this HttpRequest request)
        => request.Headers?.UserAgent;
    /// <summary>
    /// 获取请求的 Headers 值
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string? Referrer(this HttpRequest request)
        => request.Headers?.Referer;

    /// <summary>
    /// 读取请求的body值为字符串
    ///     1、在api请求过程中，确保body能够被重复读取
    ///     2、可通过启用【<see cref="ApplicationBuilderExtensions.UseRereadRequestBody(IApplicationBuilder)"/>】中间件完成
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async static Task<string> ReadStringBody(this HttpRequest request)
    {
        //  做一下补偿，万一外面没启用呢直接设置body位置会报错
        /*System.NotSupportedException: Specified method is not supported.at Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestStream.set_Position(Int64 value)*/
        request.EnableBuffering();
        try
        {
            request.Body.Position = 0;
            //   这个地方不能用using，request.body对象会被多次使用，这里using StreamReader 会导致request.body被disposed
            /*      StreamReader传入stream时，二者为引用关系
             *          禁止使用：using (StreamReader reader = new StreamReader(request.Body))
             *          1、StreamReader销毁时，会自动调用stream的Close方法<销毁，实际调用Stream的dispose方法>
             *          2、为避免stream回收导致的问题，这里读取数据时，最好将body拷贝一份出来，这样才是最保险，但也耗费内存，暂时保持现状
             */
            //  同步read不再被允许调用。Synchronous operations are disallowed. Call ReadAsync or set AllowSynchronousIO to true instead.
            string str = await new StreamReader(request.Body).ReadToEndAsync();
            return str;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
    #endregion
}