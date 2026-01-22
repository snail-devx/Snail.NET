using Snail.Aspect.General.Interfaces;
using System.Collections.Generic;

namespace Snail.Aspect.General.Components;

/// <summary>
/// 方法执行时的上下文；实现方法执行拦截，切面注入逻辑
/// <para>1、配合<see cref="IMethodInterceptor"/>使用 </para>
/// <para>2、记录方法名称、参数等信息 </para>
/// <para>3、存储、修改方法返回值数据 </para>
/// </summary>
public sealed class MethodRunContext
{
    #region 属性变量
    /// <summary>
    /// 执行的方法名称
    /// </summary>
    public readonly string Method;

    /// <summary>
    /// 方法传入的参数
    /// </summary>
    public readonly IDictionary<string, object?>? Parameters;

    /// <summary>
    /// 执行方法的返回值；若方法为void或者Task，则无返回值
    /// </summary>
    public object? ReturnValue { set; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="method"></param>
    /// <param name="parameters"></param>
    public MethodRunContext(string method, IDictionary<string, object?>? parameters)
    {
        Method = method;
        Parameters = parameters;
    }
    #endregion
}
