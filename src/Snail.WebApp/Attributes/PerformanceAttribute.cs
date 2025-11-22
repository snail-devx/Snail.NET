using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：API性能追踪
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PerformanceAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用性能追踪
    /// <para>1、为false时，在api进入时进行计时，api请求结束后停止计时</para>
    /// <para>2、为true时，不启用性能追踪</para>
    /// </summary>
    public bool Disabled { init; get; }
}