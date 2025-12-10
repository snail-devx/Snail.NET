namespace Snail.WebApp.Components;

/// <summary>
/// 上下文中间件
/// <para>1、为每个请求构建全新的运行时上下文，互不干扰</para>
/// </summary>
[Component<RunContextMiddleware>]
public class RunContextMiddleware : IMiddleware
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
        //  构建全新的运行时上下文
        RunContext rt = RunContext.New();
        Initialize(rt, context);
        //  进入下一个操作
        return next.Invoke(context);
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 请求初始化【运行时上下文】
    /// </summary>
    /// <param name="http">HTTP请求对象</param>
    /// <param name="context">全新的运行时上下文</param>
    protected virtual void Initialize(in RunContext context, in HttpContext http)
    {
        //  目前不做任何操作，后期考虑从cookie中获取共享数据写入运行时上下文
    }
    #endregion
}