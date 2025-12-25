using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Distribution.Attributes;

namespace Snail.Aspect.Distribution.Utils;

/// <summary>
/// 分布式助手类
/// <para>1、配合语法分析使用</para>
/// </summary>
internal static class DistributionHelper
{
    #region 属性变量

    /// <summary>
    /// 类型名：<see cref="ExpireAttribute"/>
    /// </summary>
    static readonly string TYPENAME_ExpireAttribute = typeof(ExpireAttribute).FullName!;
    #endregion

    #region 通用方法
    /// <summary>
    /// 获取过期时间
    /// </summary>
    /// <param name="method"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public static AttributeArgumentSyntax? GetExpireSeconds(MethodDeclarationSyntax method, SourceGenerateContext context)
    {
        AttributeSyntax? attr = method.AttributeLists.GetAttribute(context.Semantic, TYPENAME_ExpireAttribute);
        if (attr != null)
        {
            foreach (var ag in attr.GetArguments())
            {
                if (ag.NameEquals?.Name?.Identifier.ValueText == nameof(ExpireAttribute.Seconds))
                {
                    return ag;
                }
            }
        }
        return null;
    }
    #endregion

    #region 缓存相关

    #endregion

    #region 分布式锁相关

    #endregion
}