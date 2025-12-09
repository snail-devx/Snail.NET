namespace Snail.WebApp.Components;
/// <summary>
/// 遥测中间件
/// <para>1、读取http的header、cookie等信息，分析出标准化的遥测数据</para>
/// <para>2、这里不做日志写入，在<see cref="ActionBaseFilter"/>组件中进行日志详细写入</para>
/// </summary>
[Component<TelemetryMiddleware>]
public class TelemetryMiddleware : IMiddleware
{
    #region IMiddleware
    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
    {
        Initialize(RunContext.Current, context);
        return next.Invoke(context);
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 请求初始化【运行时上下文】
    /// </summary>
    /// <param name="http"></param>
    /// <param name="context"></param>
    protected virtual void Initialize(RunContext context, HttpContext http)
    {
        //  分析请求中的 标准化参数，构建 trace-id和parent-span-id
        context.InitTelemetry
        (
            traceId: http.Request.Headers[CONTEXT_TraceId],
            parentSpanId: http.Request.Headers[CONTEXT_ParentSpanId]
        );
    }
    #endregion
}