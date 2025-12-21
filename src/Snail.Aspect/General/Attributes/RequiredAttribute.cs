using System;

namespace Snail.Aspect.General.Attributes;

/// <summary>
/// 特性标签：必填（null+长度）验证
/// <para>1、null验证：如 int?，class和interface；为null验证失败 </para>
/// <para>2、null+长度验证：string、Array、List、Dictionary，为null或者空对象时验证失败 </para>
/// <para>2、配合<see cref="ValidateAspectAttribute"/>使用 </para>
/// </summary>
/// <remarks>仅支持在方法参数中，构造方法中的参数、属性、字段标记暂不支持</remarks>

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class RequiredAttribute : Attribute
{
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="message">验证失败时的错误消息</param>
    public RequiredAttribute(string? message = null)
    {
    }
}
