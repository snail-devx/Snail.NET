using System;

namespace Snail.Aspect.Web.Attributes;

/// <summary>
/// 特性标签：HTTP请求方法
/// <para>1、标记此方法是发送Http请求，标注出具体的url地址和 </para>
/// <para>2、配合<see cref="HttpAspectAttribute"/>使用 </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpMethodAttribute : Attribute
{
    /// <summary>
    /// 请求方法：Get、Post、、、
    /// </summary>
    public Enumerations.HttpMethodType Method { set; get; } = Enumerations.HttpMethodType.Get;

    /// <summary>
    /// 请求Url地址，目标服务器<see cref="HttpAspectAttribute"/>
    /// </summary>
    public string Url { set; get; }
}
