using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.Distribution.Attributes;
using Snail.Aspect.Distribution.Interfaces;
using Snail.Aspect.Distribution.Utils;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;
using static Snail.Aspect.Distribution.Utils.DistributionHelper;

namespace Snail.Aspect.Distribution;

/// <summary>
/// 【LockAspect】语法节点源码中
///  <para>1、侦测打了<see cref="LockAspectAttribute"/>标签的class和interface节点，为其生成实现class，并注册为组件 </para>
///  <para>2、自动为方法加入【分布式并发锁】控制代码 </para>
/// </summary>
internal class LockSyntaxMiddleware : ITypeDeclarationMiddleware
{
    #region 属性变量
    /// <summary>
    /// 名称：本地方法名称
    /// </summary>
    protected const string NAME_LocalMethod = "_LockNextCodeMethod";
    /// <summary>
    /// 类型名：<see cref="LockAspectAttribute"/>
    /// </summary>
    protected static readonly string TYPENAME_LockAspectAttribute = typeof(LockAspectAttribute).FullName!;
    /// <summary>
    /// 类型名：<see cref="LockMethodAttribute"/>
    /// </summary>
    protected static readonly string TYPENAME_LockMethodAttribute = typeof(LockMethodAttribute).FullName!;
    /// <summary>
    /// 类型名：<see cref="ILockAnalyzer"/>
    /// </summary>
    protected static readonly string TYPENAME_ILockAnalyzer = typeof(ILockAnalyzer).FullName!;
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
        //       这几个为了方便内部判断，如IsNullOrEmpty
        "static Snail.Utilities.Common.Utils.ArrayHelper",
        "static Snail.Utilities.Common.Utils.ObjectHelper",
        "static Snail.Utilities.Common.Utils.ExceptionHelper",
        "static Snail.Utilities.Common.Utils.StringHelper",
        "static Snail.Utilities.Collections.Utils.DictionaryHelper",
        "static Snail.Utilities.Collections.Utils.ListHelper",
        //  依赖注入相关：将生成的class注册为Interface实现组件
        "Snail.Abstractions.Dependency.Attributes",//       typeof(InjectAttribute).Namespace,//                
        "Snail.Abstractions.Dependency.Enumerations",//     typeof(LifetimeType).Namespace,//                   
        //  并发锁处理实现时所需接口
        "Snail.Abstractions.Distribution",//                typeof(ICacher).Namespace,//                        
        "Snail.Abstractions.Distribution.Attributes",//     typeof(CacherAttribute).Namespace,//                
        "Snail.Abstractions.Distribution.Exceptions",//     typeof(LockException).Namespace,//               
        "Snail.Abstractions.Distribution.Extensions",//     typeof(CacherExtensions).Namespace,//              
        //  并发锁 切面编程相关命名空间
        typeof(LockAspectAttribute).Namespace!,
        typeof(ILockAnalyzer).Namespace!,
    ];

    /// <summary>
    /// [LockAspect]特性标签
    /// </summary>
    protected readonly AttributeSyntax ANode;
    /// <summary>
    /// Lock分析器参数：<see cref="ILockAnalyzer"/>分析缓存相关Key
    /// </summary>
    protected readonly AttributeArgumentSyntax? AnalyzerArg;
    /// <summary>
    /// 是否需要【辅助】代码
    /// </summary>
    private bool _needAssistantCode = false;
    #endregion

    #region 构造方法
    /// <summary>
    /// 私有构造方法
    /// </summary>
    /// <param name="aNode"></param>
    private LockSyntaxMiddleware(AttributeSyntax aNode)
    {
        ANode = aNode;
        aNode.HasAnalyzer(out AnalyzerArg);
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
        //  仅针对“有【LockAspectAttribute】属性标记的interface和class”做处理
        if (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax)
        {
            AttributeSyntax? attr = node.AttributeLists.GetAttribute(semantic, TYPENAME_LockAspectAttribute);
            return attr != null
                ? new LockSyntaxMiddleware(attr)
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
        //  自身不能实现 [ILockAnalyzer]；若[LockAspect]指定的Analyzer也是当前类型自身，则会造成依赖注入构建实例时死循环
        context.DisableImplementAspect("CacheAspect", TYPENAME_ILockAnalyzer);
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
        //  1、无【LockMethod】属性标记，直接执行下一步逻辑
        AttributeSyntax? attr = method.AttributeLists.GetAttribute(context.Semantic, TYPENAME_LockMethodAttribute);
        if (attr == null)
        {
            string? nextCode = next?.Invoke(method, context, options);
            return nextCode;
        }
        //  2、实现前的基础验证；
        context.Generated = true;
        string? key, value, tryCount, expireSeconds;
        {
            //  方法必须是异步的：强制规则，推进异步编程
            if (options.IsAsync == false)
            {
                context.ReportError("[LockMethod]标记方法必须为异步：返回值为Task/Task<T>", method.ReturnType);
                return null;
            }
            if (CheckLockMethodAttr(context, attr, out key, out value, out tryCount) == false)
            {
                return null;
            }
            //  分析锁过期时间
            AttributeArgumentSyntax? expire = GetExpireSeconds(method, context);
            expireSeconds = expire == null ? null : $"{expire.Expression}";
            //  检测参数
            ForEachMethodParametes(method, context);
        }
        //  3、生成实现代码；先构建nextRunCode：无实际业务代码，直接空实现
        StringBuilder builder = new();
        _needAssistantCode = true;
        string? nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod, simpleBaseCall: false);
        if (string.IsNullOrEmpty(nextRunCode) == true)
        {
            builder.Append(context.LinePrefix).AppendLine("//   执行并发锁时，无NextCode代码，无需加锁，进行空实现");
            builder.Append(context.LinePrefix).AppendLine("await Task.Yield();");
            builder.Append(context.LinePrefix).AppendLine(options.ReturnType == null ? "returnl;" : "return default!;");
        }
        else
        {
            //  根据进行lockKey、lockValue参数解析
            string? tmpCode = null, lockKey = null, lockValue = null;
            if (AnalyzerArg != null)
            {
                lockKey = context.GetVarName("lockKey");
                lockValue = context.GetVarName("lockValue");
                builder.Append(context.LinePrefix).AppendLine($"string {lockKey} = {key}, {lockValue} = {value};");
                tmpCode = context.GetMethodParameterMapName(method);
                builder.Append(context.LinePrefix).AppendLine($"_lockAnalyzer.Analysis(ref {lockKey}, ref {lockValue}, {tmpCode});");
            }
            else
            {
                lockKey = key;
                lockValue = value;
            }
            //  执行加锁逻辑
            {
                List<string> parameters = [lockKey!, lockValue!, NAME_LocalMethod];
                parameters.TryAdd(tryCount == null ? null : $"tryCount:{tryCount}");
                parameters.TryAdd(expireSeconds == null ? null : $"expireSeconds:{expireSeconds}");
                tmpCode = string.Join(',', parameters);
                _ = options.ReturnType == null
                    ? builder.Append(context.LinePrefix)
                             .AppendLine($"RunResult rt = await _locker.Run({tmpCode});")
                    : builder.Append(context.LinePrefix)
                             .AppendLine($"RunResult<{options.ReturnType}> rt = await _locker.Run<{options.ReturnType}>({tmpCode});");
            }
            //  解析执行结果，报错则throw出去
            builder.Append(context.LinePrefix).AppendLine("TryThrow(rt.Exception);");
            if (options.ReturnType != null)
            {
                builder.Append(context.LinePrefix).AppendLine("return rt.Data;");
            }
        }

        context.AddGeneratedMiddleware("[LockAspect]");
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
        /** 辅助代码示例：_lockAnalyzer按需生成
        
        //  生成[LockAspect]辅助代码;
        [Locker, Server(Workspace = "Test", Code = "Default")]
        private ILocker? _locker { init; get; }
        [Inject(Key = "dddddd")]
        private ILockAnalyzer? _lockAnalyzer { init; get; }
        */
        if (_needAssistantCode == true)
        {
            context.AddNamespaces(FixedNamespaces);

            StringBuilder builder = new();
            builder.Append(context.LinePrefix).AppendLine("//  生成[LockAspect]辅助代码;");
            //  生成 ILocker 注入代码；加入必填验证
            string serverInjectCode = BuildServerInjectCodeByAttribute(ANode, context);
            builder.Append(context.LinePrefix).AppendLine($"[Locker, {serverInjectCode}]")
                   .Append(context.LinePrefix).AppendLine("private ILocker? _locker { init; get; }");
            context.AddRequiredField("_locker", "_locker为null，无法进行Lock操作");

            //  生成 ILockAnalyzer 注入代码，加入必填字段验证
            if (GenerateInjectAssistantCode(builder, context, AnalyzerArg, nameof(ILockAnalyzer), "_lockAnalyzer") == true)
            {
                context.AddRequiredField("_lockAnalyzer", "_lockAnalyzer为null，无法进行Lock操作分析");
            }

            return builder.ToString();
        }
        return null;

    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 检测【LockMethod】属性合法性
    /// </summary>
    /// <param name="context"></param>
    /// <param name="attr"></param>
    /// <param name="key">out参数：锁key</param>
    /// <param name="value">out参数：锁value</param>
    /// <param name="tryCount">out参数：尝试次数</param>
    /// <returns></returns>
    private static bool CheckLockMethodAttr(SourceGenerateContext context, AttributeSyntax attr, out string? key, out string? value, out string? tryCount)
    {
        key = value = tryCount = null;
        //  LockMethod属性验证；解析出具体的值
        AttributeArgumentSyntax? keyArg = null, valueArg = null;
        foreach (var arg in attr.GetArguments())
        {
            switch (arg.NameEquals?.Name.Identifier.ValueText)
            {
                case "Key":
                    keyArg = arg;
                    key = $"{arg.Expression}";
                    break;
                case "Value":
                    value = $"{arg.Expression}";
                    valueArg = arg;
                    break;
                case "TryCount":
                    tryCount = $"{arg.Expression}";
                    break;
                default: break;
            }
        }
        context.ReportErrorIf(SyntaxExtensions.IsNullOrEmpty(keyArg), "[LockMethod]标签传入了null/空的 Key 值", attr);
        context.ReportErrorIf(SyntaxExtensions.IsNullOrEmpty(valueArg), "[LockMethod]标签传入了null/空的 Value 值", attr);

        return key?.Length > 0 && value?.Length > 0;
    }
    #endregion
}
