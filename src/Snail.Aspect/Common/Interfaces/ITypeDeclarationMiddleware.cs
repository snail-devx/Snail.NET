using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;

namespace Snail.Aspect.Common.Interfaces;

/// <summary>
/// 接口约束：【类型定义】语法节点源码插件，用于源码生成
/// </summary>
internal interface ITypeDeclarationMiddleware
{
    /// <summary>
    /// 准备生成：做一下信息初始化，或者将将一些信息加入上下文
    /// </summary>
    /// <param name="context"></param>
    void PrepareGenerate(SourceGenerateContext context);

    /// <summary>
    /// 生成方法代码；仅包括方法内部代码
    /// </summary>
    /// <param name="method">方法语法节点</param>
    /// <param name="context">上下文对象</param>
    /// <param name="next">下一步操作；若为null则不用继续执行，返回即可</param>
    /// <param name="options">方法生成配置选项</param>
    /// <remarks>若不符合自身业务逻辑</remarks>
    /// <returns>代码字符串</returns>
    string GenerateMethodCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate next);

    /// <summary>
    /// 生成<see cref="GenerateMethodCode"/>的辅助
    /// <para>1、多个方法用到的通用逻辑，抽取成辅助方法  </para>
    /// <para>2、方法实现所需的依赖注入变量 </para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    string GenerateAssistantCode(SourceGenerateContext context);
}
