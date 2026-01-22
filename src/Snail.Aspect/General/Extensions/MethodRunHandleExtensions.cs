using Snail.Aspect.General.Components;
using Snail.Aspect.General.Interfaces;
using System;
using System.Threading.Tasks;

namespace Snail.Aspect.General.Extensions;

/// <summary>
/// <see cref="IMethodInterceptor"/>扩展方法
/// </summary>
public static class MethodRunHandleExtensions
{
    #region 公共方法
    /// <summary>
    /// 拦截异步方法；支持返回返回值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="interceptor"></param>
    /// <param name="next"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static async Task<T?> InterceptAsync<T>(this IMethodInterceptor interceptor, Func<Task<T?>> next, MethodRunContext context)
    {
        async Task interceptTask() => context.ReturnValue = await next.Invoke();
        await interceptor.InterceptAsync(interceptTask, context);
        return (T?)context.ReturnValue;
    }

    /// <summary>
    /// 拦截同步方法；支持返回返回值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="interceptor"></param>
    /// <param name="next"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static T? Intercept<T>(this IMethodInterceptor interceptor, Func<T?> next, MethodRunContext context)
    {
        void interceptAction() => context.ReturnValue = next.Invoke();
        interceptor.Intercept(interceptAction, context);
        return (T?)context.ReturnValue;
    }
    #endregion
}
