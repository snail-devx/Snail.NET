namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：Action动作基础标签
/// <para>约束API动作标签，如api，devapi，openapi、、、</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ActionAttribute : Attribute
{
    /// <summary>
    /// 动作名称
    /// <para>不传入时，基于控制器分析名称</para>
    /// </summary>
    public string? Name { init; get; }

    /// <summary>
    /// 动作标签
    /// </summary>
    public string? Tag { init; get; }
}
