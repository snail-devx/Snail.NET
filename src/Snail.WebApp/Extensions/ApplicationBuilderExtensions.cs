namespace Snail.WebApp.Extensions;

/// <summary>
/// <see cref="IApplicationBuilder"/>扩展方法
/// </summary>
public static class ApplicationBuilderExtensions
{
    #region 属性变量

    #endregion

    #region 公共方法
    /// <summary>
    /// 允许跨域URL请求
    ///     1、为了和net自带UseCors方法区分，这里命名为“UseUrlCors”
    ///     2、api的response.header中增加跨域规则
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseUrlCors(this IApplicationBuilder builder)
    {
        builder.Use((HttpContext context, RequestDelegate next) =>
        {
            /* 参照：https://blog.csdn.net/u011511086/article/details/115001095 
             * options时直接返回了，但正常应该继续往下走，用于判定API是否真实存在；后续优化
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
                    await context.Response.CompleteAsync();
                }
                else
                {
                    //允许处理跨域
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
                    context.Response.Headers.Add("Access-Control-Allow-Methods", "*");
                    await _Next(context);
                }
             */

            //  这种方式设置后，最大限度进行跨域请求
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Headers"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            //  若是options请求，直接返回了；否则进入下一个管道处理
            return context.Request.Method == "OPTIONS"
                ? context.Response.CompleteAsync()
                : next(context);
        });
        return builder;
    }
    /// <summary>
    /// 启用 请求提交数据 重复读取功能 <br />
    ///     1、解决actionfilter取不到request.Body数据的问题
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRereadRequestBody(this IApplicationBuilder builder)
    {
        //  判断请求提交数据时application/json时，才允许重读；暂时不判断post方法
        //      again, if you download/upload large files, this should be disabled to avoid memory issues
        //       https://stackoverflow.com/questions/40494913/how-to-read-request-body-in-an-asp-net-core-webapi-controller
        return builder.Use((HttpContext context, RequestDelegate next) =>
        {
            if (context.Request.HasJsonContentType() == true)
            {
                try { context.Request.EnableBuffering(); }
                catch { }
            }
            return next.Invoke(context);
        });
    }
    #endregion
}