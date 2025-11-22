using Snail.WebApp.Enumerations;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：API令牌认证、鉴权
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用鉴权认证
    /// </summary>
    public bool Disabled { init; get; }

    /// <summary>
    /// 鉴权令牌数据来源类型
    /// <para>默认值：<see cref="TokenFromType.Route"/></para>
    /// </summary>
    public TokenFromType TokenFrom { init; get; } = TokenFromType.Route;
    /// <summary>
    /// 令牌名
    /// <para>1、从哪个参数中取Token令牌值。如令牌从路由取时，路由参数名是什么。</para>
    /// <para>2、请不要带空格，使用格式会强制去掉</para>
    /// <para>默认值：tokenId</para>
    /// </summary>
    public string? TokenName { init; get; }
}