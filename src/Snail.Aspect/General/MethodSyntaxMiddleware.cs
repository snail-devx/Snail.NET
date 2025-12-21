using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.General.Attributes;
using Snail.Aspect.General.Components;
using Snail.Aspect.General.Extensions;
using Snail.Aspect.General.Interfaces;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;

namespace Snail.Aspect.General;

/// <summary>
/// [GeneralAspect]语法节点通用源码中间件
/// <para>1、侦测打了<see cref="MethodAspectAttribute"/>标签的interface、class，为其生成实现class，并注册为组件 </para>
/// <para>2、自动分析可重写实现的方法，拦截方法执行<see cref="IMethodRunHandle.OnRun"/>或者<see cref="IMethodRunHandle.OnRunAsync"/>方法 </para>
/// </summary>
internal class MethodSyntaxMiddleware : ITypeDeclarationMiddleware
{
    #region 属性变量
    /// <summary>
    /// 名称：本地方法名称
    /// </summary>
    protected const string NAME_LocalMethod = "_AspectNextCodeMethod";
    /// <summary>
    /// 类型名：<see cref="MethodAspectAttribute"/>
    /// </summary>
    protected static readonly string TYPENAME_MethodAspectAttribute = typeof(MethodAspectAttribute).FullName!;
    /// <summary>
    /// 类型名：<see cref="IMethodRunHandle"/>
    /// </summary>
    protected static readonly string TYPENAME_IMethodRunHandle = typeof(IMethodRunHandle).FullName!;
    /// <summary>
    /// 固定需要引入的命名空间集合
    /// </summary>
    protected static readonly IReadOnlyList<string> FixedNamespaces =
    [
        //  全局依赖的
        typeof(Task).Namespace!,//                           System
        "Snail.Utilities.Common",//                         
        "Snail.Utilities.Common.Utils",//                   typeof(ObjectHelper).Namespace,           
        "Snail.Utilities.Collections.Utils",//              typeof(ListHelper).Namespace,//
        "Snail.Utilities.Common.Extensions",
        "Snail.Utilities.Collections.Extensions",
        "static Snail.Utilities.Common.Utils.ObjectHelper",
        //  切面编程相关命名空间
        typeof(MethodAspectAttribute).Namespace!,
        typeof(IMethodRunHandle).Namespace!,
        typeof(MethodRunHandleExtensions).Namespace!,
        typeof(MethodRunContext).Namespace!,
    ];

    /// <summary>
    /// [CacheAspect]特性标签
    /// </summary>
    protected readonly AttributeSyntax ANode;
    /// <summary>
    /// 缓存分析器参数：<see cref="IMethodRunHandle"/>分析缓存相关Key
    /// </summary>
    protected readonly AttributeArgumentSyntax? RunHandleArg;
    /// <summary>
    /// 是否需要【辅助】代码
    /// </summary>
    private bool _needAssistantCode = false;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="aNode"></param>
    private MethodSyntaxMiddleware(AttributeSyntax aNode)
    {
        ANode = aNode;
        aNode.HasArgument("RunHandle", out RunHandleArg);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 构建中间件
    /// </summary>
    /// <param name="node"></param>
    /// <param name="semantic"></param>
    /// <returns></returns>
    public static ITypeDeclarationMiddleware? Build(TypeDeclarationSyntax node, SemanticModel semantic)
    {
        if (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax)
        {
            AttributeSyntax? attr = node.AttributeLists.GetAttribute(semantic, TYPENAME_MethodAspectAttribute);
            return attr != null
                ? new MethodSyntaxMiddleware(attr)
                : null;
        }
        return null;
    }
    #endregion

    #region ITypeDeclarationMiddleware
    /// <summary>
    /// 准备生成：做一下信息初始化，或者将将一些信息加入上下文
    /// </summary>
    /// <param name="context"></param>
    void ITypeDeclarationMiddleware.PrepareGenerate(SourceGenerateContext context)
    {
        context.ReportErrorIf
        (
            condition: RunHandleArg == null || $"{RunHandleArg.Expression}" == "null",
            message: $"[MethodAspect]必须传入RunHandle值，且不能为null",
            syntax: ANode
        );
        //  不支持泛型类型标记[MethodAspect]；可能导致分析类型失败，先简化强制禁用
        context.DisableGenericAspect("MethodAspect");
        //  自身不能实现 [IMethodRunHandle]；若[MethodAspect]指定的RunHandle也是当前类型自身，则会造成依赖注入构建实例时死循环
        context.DisableImplementAspect("CacheAspect", TYPENAME_IMethodRunHandle);
    }
    /// <summary>
    /// 生成方法代码；仅包括方法内部代码
    /// </summary>
    /// <param name="method">方法语法节点</param>
    /// <param name="context">上下文对象</param>
    /// <param name="options">方法生成配置选项</param>
    /// <param name="next">下一步操作；若为null则不用继续执行，返回即可</param>
    /// <remarks>若不符合自身业务逻辑</remarks>
    /// <returns>代码字符串</returns>
    string? ITypeDeclarationMiddleware.GenerateMethodCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate? next)
    {
        //  是否能够重写；无法重写的方法直接忽略掉
        {
            //  类方法除非指定Abstract或者Virtual，否则不与重写拦截
            if (context.TypeIsClass && options.IsVirtual == false && options.IsAbstract == false)
            {
                return null;
            }
            //  接口的静态方法，忽略掉
            if (context.TypeIsClass == false && options.IsStatic == true)
            {
                return null;
            }
        }
        //  分析下一步执行代码；若无具体代码逻辑，则报错提示出来
        string? nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod, simpleBaseCall: false);
        if (string.IsNullOrEmpty(nextRunCode) == true)
        {
            context.ReportError
            (
                message: $"无实际代码，无法进行[MethodAspect]拦截，可配合[HttpAspect]等标签自动生成代码",
                syntax: method
            );
            return null;
        }
        //  生成面向切面的相关代码
        StringBuilder builder = new();
        context.Generated = true;
        _needAssistantCode = true;
        //      初始化context
        builder.Append(context.LinePrefix)
               .Append($"{nameof(MethodRunContext)} mrhContext = new(")
               .Append($"\"{method.Identifier}\", ")
               .Append(context.GetMethodParameterMapName(method))
               .AppendLine(");");
        //      生成执行代码：需要区分是否有返回值；配合 IMethodRunHandle 扩展方法，简化代码逻辑；替换下面的旧代码
        builder.Append(context.LinePrefix)
               .Append(options.ReturnType == null ? string.Empty : $"return ")
               .Append(options.IsAsync ? $"await _aspectRunHandle!.OnRunAsync" : $"_aspectRunHandle.OnRun")
               .Append(options.ReturnType == null ? string.Empty : $"<{options.ReturnType}>")
               .Append('(').Append(NAME_LocalMethod).AppendLine(", mrhContext);");

        return builder.ToString();
    }
    /// <summary>
    /// 生成<see cref="ITypeDeclarationMiddleware.GenerateMethodCode"/>的辅助
    /// <para>1、多个方法用到的通用逻辑，抽取成辅助方法  </para>
    /// <para>2、方法实现所需的依赖注入变量 </para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    string? ITypeDeclarationMiddleware.GenerateAssistantCode(SourceGenerateContext context)
    {
        /** 生成的辅助代码样例：

        //  生成[MethodAspect]辅助代码;
        [Inject(Key = "111")]
        private IMethodRunHandle? _aspectRunHandle { init; get; }
        */
        if (_needAssistantCode == true)
        {
            context.AddNamespaces(FixedNamespaces);
            //  直接生成，在【PrepareGenerate】判断了RunHandleArg必须存在且有效
            StringBuilder builder = new();
            builder.Append(context.LinePrefix).AppendLine("//  生成[MethodAspect]辅助代码;");
            GenerateInjectAssistantCode(builder, context, RunHandleArg, nameof(IMethodRunHandle), "_aspectRunHandle");
            context.AddRequiredField("_aspectRunHandle", $"_aspectRunHandle为null，无法进行Aspect操作");
            return builder.ToString();
        }
        return null;
    }
    #endregion
}

