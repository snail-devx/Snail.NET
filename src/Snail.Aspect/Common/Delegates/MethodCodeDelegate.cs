using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;

namespace Snail.Aspect.Common.Delegates;

/// <summary>
/// 方法代码生成委托
/// </summary>
/// <param name="method">方法节点</param>
/// <param name="context">源码生成上下文</param>
/// <param name="options">方法生成配置选项</param>
/// <returns></returns>
internal delegate string MethodCodeDelegate(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options);
