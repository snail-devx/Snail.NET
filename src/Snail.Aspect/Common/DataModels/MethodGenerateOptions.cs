using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Snail.Aspect.Common.DataModels;

/// <summary>
/// 【方法】代码生成配置选项
/// </summary>
internal readonly struct MethodGenerateOptions
{
    #region 属性变量
    /// <summary>
    /// 方法的访问修饰符：public、private、internal等
    /// </summary>
    public IReadOnlyList<SyntaxToken> AccessTokens { get; }
    /// <summary>
    /// 私有方法
    /// </summary>
    public bool IsPrivate { get; }

    /// <summary>
    /// 静态方法
    /// </summary>
    public bool IsStatic { get; }
    /// <summary>
    /// 密封方法
    /// </summary>
    public bool IsSealed { get; }
    /// <summary>
    /// 抽象方法
    /// </summary>
    public bool IsAbstract { get; }
    /// <summary>
    /// 虚拟方法
    /// </summary>
    public bool IsVirtual { get; }

    /// <summary>
    /// 异步方法；返回值为task
    /// </summary>
    public bool IsAsync { get; }
    /// <summary>
    /// 方法返回值类型
    /// <para>1、若为异步方法，则为Task的泛型参数值，如<see cref="Task{String}"/>，则是<see cref="string"/> </para>
    /// <para>2、若为null，则为void方法 </para>
    /// </summary>
    public TypeSyntax ReturnType { get; }

    /// <summary>
    /// 是否是显示接口实现方法
    /// </summary>
    public bool ExplicitInterface { get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法；内部自动分析相关属性
    /// </summary>
    /// <param name="method"></param>
    /// <param name="context"></param>
    public MethodGenerateOptions(MethodDeclarationSyntax method, SourceGenerateContext context)
    {
        //  默认值初始化
        {
            IsPrivate = false;
            IsStatic = false;
            IsSealed = false;
            IsAbstract = false;
            IsVirtual = false;
            IsAsync = false;
            ReturnType = null;
        }
        //  基于访问修饰符初始化
        List<SyntaxToken> accessTokens = [];
        foreach (var token in method.Modifiers)
        {
            switch (token.Kind())
            {
                case SyntaxKind.StaticKeyword:
                    IsStatic = true;
                    break;
                case SyntaxKind.SealedKeyword:
                    IsSealed = true;
                    break;
                case SyntaxKind.AbstractKeyword:
                    IsAbstract = true;
                    break;
                case SyntaxKind.VirtualKeyword:
                    IsVirtual = true;
                    break;
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                    accessTokens.Add(token);
                    break;
                case SyntaxKind.PrivateKeyword:
                    accessTokens.Add(token);
                    IsPrivate = true;
                    break;
            }
        }
        AccessTokens = new ReadOnlyCollection<SyntaxToken>(accessTokens);
        //  返回值构建；并处理命名空间
        ReturnType = method.GetRealReturnType(context.Semantic, out var isAsync, out var ns);
        IsAsync = isAsync;
        context.AddNamespaces(ns);

        //  显示接口实现方法
        ExplicitInterface = context.Semantic.GetDeclaredSymbol(method).ExplicitInterfaceImplementations.Length > 0;
    }
    #endregion
}