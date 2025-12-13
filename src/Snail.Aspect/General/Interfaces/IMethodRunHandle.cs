using Snail.Aspect.General.Components;
using System;
using System.Threading.Tasks;

namespace Snail.Aspect.General.Interfaces;

/// <summary>
/// 接口：方法运行句柄，实现此接口的类型下的方法运行时自动进行方法拦截
/// <para> 1、配合<see cref="Attributes.MethodAspectAttribute"/>实现对有<see cref="IMethodRunHandle"/>接口的类型进行切面注入 </para>
/// </summary>
public interface IMethodRunHandle
{
    /// <summary>
    /// 异步方法运行时
    /// <para>1、若方法有返回值；则执行<paramref name="next"/>后，可通过<paramref name="context"/>.ReturnValue 查看执行后的返回值 </para>
    /// <para>2、若需要在执行<paramref name="next"/>后修改返回值，可通过<paramref name="context"/>.SetReturnValue 方法实现 </para>
    /// </summary>
    /// <param name="next">下一个动作代码委托</param>
    /// <param name="context">方法运行的上下文参数</param>
    /// <returns></returns>
    Task OnRunAsync(Func<Task> next, MethodRunContext context);

    /// <summary>
    /// 同步方法运行时
    /// <para>1、若方法有返回值；则执行<paramref name="next"/>后，可通过<paramref name="context"/>.ReturnValue 查看执行后的返回值 </para>
    /// <para>2、若需要在执行<paramref name="next"/>后修改返回值，可通过<paramref name="context"/>.SetReturnValue 方法实现 </para>
    /// </summary>
    /// <param name="next">下一个动作代码委托</param>
    /// <param name="context">方法运行的上下文参数</param>
    void OnRun(Action next, MethodRunContext context);
}
