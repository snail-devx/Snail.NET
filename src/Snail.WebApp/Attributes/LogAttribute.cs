using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：API日志记录
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class LogAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用日志记录
    /// <para>1、为false时，拦截api进行日志记录，记录请求信息、响应结果等信息</para>
    /// <para>2、为true时，调用方接收到的500错误</para>
    /// </summary>
    public bool Disabled { init; get; }

    /// <summary>
    /// 是否记录请求提交数据内容
    /// <para>默认值：true</para>
    /// </summary>
    public bool Content { init; get; } = true;
    /// <summary>
    /// 是否记录请求Header
    /// <para>默认值：true</para>
    /// </summary>
    public bool Header { init; get; } = true;
}
