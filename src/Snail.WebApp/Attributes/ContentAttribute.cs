using Snail.WebApp.Enumerations;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// API接收内容特性标签；配合完成请求content-type过滤
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ContentAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool Disabled { init; get; }

    /// <summary>
    /// 允许的content-type类型；支持多个“|”拼接
    /// </summary>
    public ContentType Allow { init; get; }
}