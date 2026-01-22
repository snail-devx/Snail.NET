using Snail.Aspect.General.Components;
using System;
using System.Threading.Tasks;

namespace Snail.Aspect.General.Interfaces;

/// <summary>
/// 接口：方法拦截器，实现此接口的类型下的方法运行时自动进行方法拦截
/// <para> 1、配合<see cref="Attributes.MethodAspectAttribute"/>实现对有<see cref="IMethodInterceptor"/>接口的类型进行切面注入 </para>
/// </summary>
public interface IMethodInterceptor
{
    /// <summary>
    /// 拦截异步方法
    /// <para>1、方法执行时若需要返回值，则通过<paramref name="context"/>属性<see cref="MethodRunContext.ReturnValue"/>属性值 </para>
    /// </summary>
    /// <param name="next">下一个动作代码委托</param>
    /// <param name="context">方法运行的上下文参数</param>
    /// <returns></returns>
    Task InterceptAsync(Func<Task> next, MethodRunContext context);

    /// <summary>
    /// 拦截同步方法
    /// <para>1、方法执行时若需要返回值，则通过<paramref name="context"/>属性<see cref="MethodRunContext.ReturnValue"/>属性值 </para>
    /// </summary>
    /// <param name="next">下一个动作代码委托</param>
    /// <param name="context">方法运行的上下文参数</param>
    void Intercept(Action next, MethodRunContext context);
}
