using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;

namespace Snail.Aspect.Common.Utils
{
    /// <summary>
    /// 语法相关中间件的助手类 <br />
    ///     1、负责为中间件实现，提供一下通用方法逻辑
    /// </summary>
    internal static class SyntaxMiddlewareHelper
    {
        #region 属性变量
        /// <summary>
        /// 类方法的【原始代码】调用委托；<br />
        ///     1、base.xxxxx
        /// </summary>
        public static readonly MethodCodeDelegate CallBaseMethodCode = (method, context, options) =>
        {
            //  后续判断是否是接口方法的显示实现，如string ISyntaxProxy.GenerateCode；此时需要进行【(base as IHttpRequest1).XXX】处理

            /* context.Generated = true;    这个不标记为已生成过了，如果没有其他处理逻辑，方法自身都不用重写*/
            // 示例：base.XXX(xxx,x,x,x,x);   异步方法则是 await base.XXX(xxx,x,x,x,x);
            StringBuilder builder = new StringBuilder();
            builder.Append(context.LinePrefix)
                   .Append(options.ReturnType == null ? string.Empty : $"return ")
                   .Append(options.IsAsync == false ? string.Empty : $"await ")
                   .Append("base.").Append(method.Identifier.Text)
                   .Append("(").Append(string.Join(", ", method.ParameterList.Parameters.Select(p => p.Identifier.Text))).AppendLine(");");

            context.AddGeneratedMiddleware(nameof(CallBaseMethodCode));
            return builder.ToString();
        };

        /// <summary>
        /// 制表符：方法的前置空白
        /// </summary>
        public const string TAB_MethodSpace = "\t\t";
        #endregion

        #region 公共方法
        /// <summary>
        /// 基于枚举值全路径，转换成对应的枚举值对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fullEnumValuePath">枚举值全路径，如Snail.Aspect.Distribution.Enumerations.CacheType.HashCache</param>
        /// <returns></returns>
        /// <remarks>仅限本程序集下的枚举类型</remarks>
        public static T GetEnumByFullEnumValuePath<T>(string fullEnumValuePath) where T : Enum
        {
            // 分离类型全名和成员名
            int lastDotIndex = fullEnumValuePath.LastIndexOf('.');
            string typeName = fullEnumValuePath.Substring(0, lastDotIndex);
            string enumMemberName = fullEnumValuePath.Substring(lastDotIndex + 1);
            // 获取类型
            Type enumType = Type.GetType(typeName) ?? throw new TypeUnloadedException("无法找到指定的类型。");
            // 尝试转换为枚举值
            object enumValue = Enum.Parse(enumType, enumMemberName);
            return (T)enumValue;
        }

        /// <summary>
        /// 基于属性节点，构建【服务器】注入代码 <br />
        ///     1、示例属性节点：[HttpAspect(Workspace = "Test", Code = "BAIDU", Analyzer = Cons.Analyzer)] <br />
        ///     2、将返回注入代码：Server(Workspace = "Test",Code = "BAIDU")；需要外部自己组装到 “[]”中
        ///     3、其他属性参数，通过<paramref name="otherArgs"/>通知外面自己处理
        /// </summary>
        /// <param name="aspectAttr"></param>
        /// <param name="context"></param>
        /// <param name="otherArgs"></param>
        /// <returns></returns>
        public static string BuildServerInjectCodeByAttribute(AttributeSyntax aspectAttr, SourceGenerateContext context, Action<string, AttributeArgumentSyntax> otherArgs = null)
        {
            AttributeArgumentSyntax code = null;
            //  遍历属性参数信息，得到Server注入参数：非Server的注入参数，执行otherArgs通知外面；并将Code独立出来，方便后面做必填验证
            List<AttributeArgumentSyntax> serverParams = aspectAttr.GetArguments()
                .Where(ag =>
                {
                    string agName = ag.NameEquals?.Name?.Identifier.ValueText;
                    switch (agName)
                    {
                        //  server相关注入参数
                        case "Workspace": return true;
                        case "Code": code = ag; return true;
                        case "Type": return true;
                        //  其他参数：执行委托通知外部自己处理
                        default:
                            otherArgs?.Invoke(agName, ag); return false;
                    }
                })
                .ToList();
            //  验证参数，生成注入代码
            context.ReportErrorIf(
                condition: Extensions.SyntaxExtensions.IsNullOrEmpty(code),
                message: $"[{aspectAttr.Name}]标签传入了null/空的Code值",
                syntax: aspectAttr
            );
            return serverParams.Count > 0
                ? $"Server({string.Join(", ", serverParams)})"
                : "Server";
        }

        /// <summary>
        /// 遍历方法参数做验证，如in、out、ref等参数和参数数据类型验证
        /// </summary>
        /// <param name="mNode"></param>
        /// <param name="context"></param>
        /// <param name="action">非null时，执行此委托</param>
        /// <param name="supportTaskParam"></param>
        /// <param name="supportRefParam"></param>
        /// <param name="supportInParam"></param>
        /// <param name="supportOutParam"></param>
        public static void ForEachMethodParametes(MethodDeclarationSyntax mNode, SourceGenerateContext context, Action<string, ParameterSyntax> action = null,
            bool supportTaskParam = false, bool supportRefParam = false, bool supportInParam = false, bool supportOutParam = false)
        {
            foreach (ParameterSyntax parameter in mNode.ParameterList.Parameters)
            {
                //  对参数的支持情况做验证
                context.ReportErrorIf(
                    condition: supportTaskParam == false && parameter.Type?.IsTaskType(context.Semantic) == true,
                    message: "不支持Task类型参数",
                    syntax: parameter
                );
                //  遍历参数的修饰符
                foreach (var mt in parameter.Modifiers)
                {
                    switch (mt.Kind())
                    {
                        case SyntaxKind.RefKeyword:
                            context.ReportErrorIf(supportRefParam == false, "不支持ref参数", parameter);
                            break;
                        case SyntaxKind.InKeyword:
                            context.ReportErrorIf(supportInParam == false, "不支持in参数", parameter);
                            break;
                        case SyntaxKind.OutKeyword:
                            context.ReportErrorIf(supportOutParam == false, "不支持out参数", parameter);
                            break;
                        default: break;
                    }
                }
                //  执行遍历委托：先整理参数相关的命名空间
                context.AddNamespaces(parameter.Type?.GetUsedNamespaces(context.Semantic));
                action?.Invoke(parameter.Identifier.Text, parameter);
            }
        }

        /// <summary>
        /// 基于<paramref name="next"/>生成执行代码；可生成两种模式的执行代码<br />
        ///     1、生成本地方法，此时外部可传入<paramref name="localMethodName"/><br />
        ///     2、生成基类代码，此时<paramref name="localMethodName"/>失效，此种情况一般为执行基类方法时，仅一行代码，生成【本地方法】不符合逻辑<br />
        ///     3、根据代码情况做自动优化；若生成本地方法，则自动加入<see cref="SourceGenerateContext.LocalMethods"/>代码中<br />
        /// </summary>
        /// <param name="method"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="next"></param>
        /// <param name="localMethodName">本地方法名称；传null则不用本地方法包括next生成代码</param>
        /// <param name="simpleBaseCall">简化base方法的调用；如果next插件生成的是执行基类方法，则做简化处理，如“base.LockTest();”，减少本地方法的生成</param>
        /// <returns>执行代码（如  base.SaveObjects(x);、await _CacheNextCodeMethod();），若生成失败，则返回null</returns>
        /// <remarks>仅在自身有同样需要生成代码时才实用，否则不推荐使用</remarks>
        public static string GenerateRunCodeWithNext(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate next, string localMethodName, bool simpleBaseCall = true)
        {
            string nextCode;
            {
                string oldLine = context.LinePrefix;
                context.LinePrefix = $"{TAB_MethodSpace}\t";
                nextCode = next?.Invoke(method, context, options)?.Trim();
                context.LinePrefix = oldLine;
            }
            //  无代码，直接null
            if (string.IsNullOrEmpty(nextCode) == false)
            {
                //  仅有【CallBaseMethodCode】中间件参与代码生成时，表示只为执行基类方法，（base.xxxx())，干掉 "return "返回，简化代码
                if (simpleBaseCall == true)
                {
                    if (context.Middlewares.Count == 1 && context.Middlewares[0] == nameof(CallBaseMethodCode))
                    {
                        return nextCode.Substring(0, "return ".Length) == "return "
                            ? nextCode.Substring("return ".Length)
                            : nextCode;
                    }
                }
                //  进行本地方法构建
                if (string.IsNullOrEmpty(localMethodName) == false)
                {
                    //  需要构建 本地方法；并加入【本地方法】代码组中
                    string modifierCode = options.ReturnType == null
                       ? (options.IsAsync ? $"async Task" : "void")
                       : (options.IsAsync ? $"async Task<{options.ReturnType}>" : $"{options.ReturnType}");
                    context.LocalMethods
                        .Append(TAB_MethodSpace).Append(modifierCode).Append(" ").Append(localMethodName).AppendLine("()")
                        .Append(TAB_MethodSpace).AppendLine("{")
                        .Append(TAB_MethodSpace).Append("\t").AppendLine(nextCode)
                        .Append(TAB_MethodSpace).AppendLine("}");
                    //      返回本地方法执行代码
                    return options.IsAsync
                        ? $"await {localMethodName}();"
                        : $"{localMethodName}();";
                }
            }

            return nextCode;
        }

        /// <summary>
        /// 生成依赖注入代码的辅助代码
        /// </summary>
        /// <param name="builder">代码构建器</param>
        /// <param name="context"></param>
        /// <param name="injectKeyArg">依赖注入属性参数</param>
        /// <param name="typeName">from类型名：如 ICacheAnalyzer</param>
        /// <param name="injectVarName">依赖注入实例变量名：如 _cacheAnalyzer</param>
        /// <returns>生成成功返回true；否则false</returns>
        /// <remarks>
        /// 生成的示例代码：<br />
        ///     [Inject(Key = "xxx")]<br />
        ///     private ICacheAnalyzer? _cacheAnalyzer { init; get; }<br />
        /// </remarks>
        public static bool GenerateInjectAssistantCode(StringBuilder builder, SourceGenerateContext context, AttributeArgumentSyntax injectKeyArg, string typeName, string injectVarName)
        {
            if (injectKeyArg != null)
            {
                string tmpCode = $"{injectKeyArg.Expression}";
                tmpCode = tmpCode == "null" ? "[Inject]" : $"[Inject(Key = {tmpCode})]";
                builder.Append(context.LinePrefix).AppendLine(tmpCode)
                       .Append(context.LinePrefix).AppendLine($"private {typeName}? {injectVarName} {{ init; get; }}");
                return true;
            }
            return false;
        }
        #endregion
    }
}
