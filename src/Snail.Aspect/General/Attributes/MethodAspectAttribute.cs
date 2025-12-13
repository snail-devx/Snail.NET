using System;

namespace Snail.Aspect.General.Attributes;

/// <summary>
/// 特性标签：标记此类、接口需要进行方法面向切面编程
/// <para>1、有此标记的class、interface方法进行自动重写，拦截方法调用 </para>
/// <para>2、结合<see cref="RunHandle"/>在运行时实现方法拦截，并调用此句柄 </para>
/// <para>3、仅拦截实例方法（可override实例方法，接口方法），不拦截静态方法 </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MethodAspectAttribute : Attribute
{
    /// <summary>
    /// 方法运行句柄
    /// <para>1、必传；实现<see cref="Interfaces.IMethodRunHandle"/>的类型依赖注入Key值 </para>
    /// </summary>
    public string RunHandle { get; set; }
}

