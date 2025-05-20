using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.Web.Attributes;
using Snail.Aspect.Web.Enumerations;
using Snail.Aspect.Web.Interfaces;
using Snail.Aspect.Web.Utils;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;


namespace Snail.Aspect.Web
{
    /// <summary>
    /// [Http]语法节点源码中间件 <br />
    ///     1、侦测打了<see cref="HttpAspectAttribute"/>标签的Interface，为其生成实现class，并注册为组件 <br />
    /// </summary>
    /// <remarks>将作为最底层插件，不会再执行next插件动作</remarks>
    internal class HttpSyntaxMiddleware : ITypeDeclarationMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 类型名：HttpAspectAttribute
        /// </summary>
        protected static readonly string TYPENAME_HttpAspectAttribute = typeof(HttpAspectAttribute).FullName;
        /// <summary>
        /// 类型名：HttpMethodAttribute
        /// </summary>
        protected static readonly string TYPENAME_HttpMethodAttribute = typeof(HttpMethodAttribute).FullName;
        /// <summary>
        /// 类型名：HttpBodyAttribute
        /// </summary>
        protected static readonly string TYPENAME_HttpBodyAttribute = typeof(HttpBodyAttribute).FullName;

        /// <summary>
        /// 固定需要引入的命名空间几何
        /// </summary>
        protected static readonly IReadOnlyList<string> FixedNamespaces = new List<string>()
        {
            //  全局依赖的
            "Snail.Utilities.Common.Utils",                      //  typeof(ObjectHelper).Namespace,//                   ,
            "static Snail.Utilities.Common.Utils.ObjectHelper",
            //  依赖注入相关：将生成的class注册为Interface实现组件
            "Snail.Abstractions.Dependency.Attributes",         //  typeof(InjectAttribute).Namespace,//                
            "Snail.Abstractions.Dependency.Enumerations",       //  typeof(LifetimeType).Namespace,//                   
            //  HTTP请求实现时所需接口
            "Snail.Abstractions.Web",//                           typeof(IHttpRequestor).Namespace,//  
            "Snail.Abstractions.Web.Attributes",//              typeof(HttpRequestorAttribute).Namespace,//  
            "Snail.Abstractions.Web.DataModels",//              typeof(HttpResult).Namespace,// 
            "Snail.Abstractions.Web.Extensions",//              typeof(HttpRequestorExtensions).Namespace,// 
            //  Web 切面编程相关命名空间
            typeof(HttpAspectAttribute).Namespace,
            typeof(HttpMethodType).Namespace,
            typeof(IHttpAnalyzer).Namespace,
            $"static {typeof(HttpAspectHelper).FullName}",
        };

        /// <summary>
        /// [Http]特性标签
        /// </summary>
        protected readonly AttributeSyntax ANode;
        /// <summary>
        /// HTTP分析器参数：<see cref="IHttpAnalyzer"/>分析缓存相关Key
        /// </summary>
        protected readonly AttributeArgumentSyntax AnalyzerArg;
        /// <summary>
        /// 是否需要【辅助】代码
        /// </summary>
        private bool _needAssistantCode = false;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        private HttpSyntaxMiddleware(AttributeSyntax aNode)
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
        public static ITypeDeclarationMiddleware Build(TypeDeclarationSyntax node, SemanticModel semantic)
        {
            //  仅针对“有【HttpAttribute】属性标记的interface和class”做处理
            if (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax)
            {
                AttributeSyntax httpAttr = node.AttributeLists.GetAttribute(semantic, TYPENAME_HttpAspectAttribute);
                return httpAttr != null
                    ? new HttpSyntaxMiddleware(httpAttr)
                    : null;
                //return new HttpSyntaxMiddleware(iNode, httpAttr);
            }
            return null;
        }
        #endregion

        #region ISourceMiddleware
        /// <summary>
        /// 准备生成：做一下信息初始化，或者将将一些信息加入上下文
        /// </summary>
        /// <param name="context"></param>
        void ITypeDeclarationMiddleware.PrepareGenerate(SourceGenerateContext context)
        {
            context.ReportErrorIf
            (
                condition: context.TypeSyntax.TypeParameterList?.Parameters.Count > 0,
                message: $"[HttpAspect]暂不支持在泛型class/interface中使用",
                syntax: context.TypeSyntax.TypeParameterList?.Parameters.First()
            );
        }

        /// <summary>
        /// 生成方法代码；仅包括方法内部代码
        /// </summary>
        /// <param name="method">方法语法节点</param>
        /// <param name="context">上下文对象</param>
        /// <param name="options">方法生成配置选项</param>
        /// <param name="next">下一步操作</param>
        /// <remarks>若不符合自身业务逻辑</remarks>
        /// <returns>代码字符串</returns>
        string ITypeDeclarationMiddleware.GenerateMethodCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate next)
        {
            /** 基本思路：
             *      1、标注了HttpMethod属性，则作为最底层插件对外提供；不会再调用next
             *          Http请求涉及返回值等处理，若在检测出其他逻辑，涉及返回值选举合并等复杂逻辑，无明确规则容易出问题
             *      2、未标注HttpMethod属性，则继续执行Next逻辑
             */
            //  1、无缓存属性标记，直接执行下一步逻辑
            AttributeSyntax methodAttr = method.AttributeLists.GetAttribute(context.Semantic, TYPENAME_HttpMethodAttribute);
            if (methodAttr == null)
            {
                string nextCode = next?.Invoke(method, context, options);
                return nextCode;
            }
            //  2、有HttpMethod标记，执行进行下一步验证
            context.Generated = true;
            {
                //  不支持泛型方法：泛型参数整理起来太复杂，且就是Http请求，没必要
                if (method.TypeParameterList?.Parameters.Count > 0)
                {
                    context.ReportError("[HttpMethod]不支持泛型方法", method.TypeParameterList.Parameters.First());
                    return null;
                }
                //  方法必须是异步的：强制规则，推进异步编程
                if (options.IsAsync == false)
                {
                    context.ReportError("[HttpMethod]标记方法必须为异步：返回值为Task/Task<T>", method.ReturnType);
                    return null;
                }
                //  如果是class，则必须是Abstract方法；否则会有返回值选举冲突，到底有http的还是base方法的
                if (context.TypeIsClass && options.IsAbstract == false)
                {
                    string message = $"[HttpMethod]标记的class，仅支持abstract方法";
                    context.ReportError(message, method);
                }
            }
            //  3、进行HTTP请求方法代码实现
            _needAssistantCode = true;
            StringBuilder builder = new StringBuilder();
            //      1、分析方法参数，得到HttpBodyAttribute参数信息；不准有out参数，不准有task参数，不会等待
            List<string> parameters = new List<string>();
            string bodyParameter = null;
            ForEachMethodParametes(method, context, (name, parameter) =>
            {
                if (parameter.AttributeLists.GetAttribute(context.Semantic, TYPENAME_HttpBodyAttribute) != null)
                {
                    context.ReportErrorIf(bodyParameter != null, "不支持多个[HttpBody]标记参数", parameter);
                    bodyParameter = name;
                }
                parameters.Add(name);
            });
            //      2、生成http请求结果；发送请求前，针对url参数做处理
            GenerateHttpRequestCode(builder, method, methodAttr, context, parameters, bodyParameter, options.ReturnType == null);
            //      3、构建返回结果：根据返回数据类型做区别处理
            GenerateMethodReturnCode(builder, options.ReturnType, context);

            context.AddGeneratedMiddleware("[HttpAspect]");
            return builder.ToString();
        }

        /// <summary>
        /// 生成<see cref="ITypeDeclarationMiddleware.GenerateMethodCode"/>的辅助 <br />
        ///     1、多个方法用到的通用逻辑，抽取成辅助方法 
        ///     2、方法实现所需的依赖注入变量 <br />
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        string ITypeDeclarationMiddleware.GenerateAssistantCode(SourceGenerateContext context)
        {
            if (_needAssistantCode == false)
            {
                return null;
            }
            //  添加需要的命名空间
            context.AddNamespaces(FixedNamespaces);
            //  生成辅助代码
            StringBuilder builder = new StringBuilder();
            builder.Append(context.LinePrefix).AppendLine("//  生成[HttpAspect]辅助代码;");
            //      解析属性标签节点：生成Server的依赖依赖注入代码
            string serverInjectCode = BuildServerInjectCodeByAttribute(ANode, context);
            builder.Append(context.LinePrefix).AppendLine($"[HttpRequestor, {serverInjectCode}]")
                   .Append(context.LinePrefix).AppendLine("private IHttpRequestor? _requestor { init; get; }");
            //      生成分析器代码
            GenerateAnalyzerAssistantCode(builder, context, AnalyzerArg, nameof(IHttpAnalyzer), "_httpAnalyzer");

            return builder.ToString();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 生成【Http】请求发送代码：内部自动处理url参数，执行【IHttpAnalyzer】逻辑
        /// </summary>
        /// <param name="builder">代码构建器，将生成代码添加进去</param>
        /// <param name="mNode">方法节点</param>
        /// <param name="methodAttr">方法属性语法节点，分析请求的url和method</param>
        /// <param name="context">代码生成上下文，用于报告错误信息</param>
        /// <param name="parameters">方法的参数名列表</param>
        /// <param name="bodyParameter">post时提交的数据参数名</param>
        /// <param name="isVoidMethod">是否是void类型方法，如void或者Task</param>
        private void GenerateHttpRequestCode(StringBuilder builder, MethodDeclarationSyntax mNode, AttributeSyntax methodAttr, SourceGenerateContext context, List<string> parameters, string bodyParameter, bool isVoidMethod)
        {
            builder.Append(context.LinePrefix).AppendLine("ThrowIfNull(_requestor, \"_requestor为null，无法进行Http请求\");");
            //  分析url和method类型
            HttpMethodType method = HttpMethodType.Get; string url;
            {
                AttributeArgumentSyntax urlArg = null;
                foreach (var arg in methodAttr.GetArguments())
                {
                    switch (arg.NameEquals?.Name.Identifier.ValueText)
                    {
                        case "Url":
                            urlArg = arg;
                            break;
                        case "Method":
                            method = GetEnumByFullEnumValuePath<HttpMethodType>($"{context.Semantic.GetSymbolInfo(arg.Expression).Symbol}");
                            break;
                        default: break;
                    }
                }
                //  url参数
                context.ReportErrorIf(SyntaxExtensions.IsNullOrEmpty(urlArg), "[HttpMethod]标签传入了null/空的 Url 值", methodAttr);
                url = $"{urlArg.Expression}";
            }
            //  url相关参数处理（执行AnalysisUrl方法）
            string urlVarName = null;
            if (AnalyzerArg != null)
            {
                urlVarName = context.GetVarName("url");
                string ampName = context.GetMethodParameterMapName(mNode);
                builder.Append(context.LinePrefix)
                       .AppendLine($"string {urlVarName} = await AnalysisHttpUrl(_httpAnalyzer, {url}, {ampName});");
            }
            //  构建http请求代码
            bodyParameter = string.IsNullOrEmpty(bodyParameter) ? "(object?)null" : bodyParameter;
            builder.Append(context.LinePrefix).Append(isVoidMethod ? "await " : "HttpResult hr = await ");
            switch (method)
            {
                //  Get请求
                case HttpMethodType.Get:
                    builder.AppendLine($"_requestor.Get({urlVarName ?? url});");
                    break;
                //  Post请求
                case HttpMethodType.Post:
                    builder.AppendLine($"_requestor.Post({urlVarName ?? url}, {bodyParameter});");
                    break;
                //  默认，暂时不支持
                default:
                    context.ReportError($"[HttpMethod]标签传入了不支持的 Method 值：{method}", methodAttr);
                    builder.AppendLine("Task.Yield();");
                    break;
            }
        }
        /// <summary>
        /// 生成方法的【返回值】代码
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="returnType"></param>
        /// <param name="context"></param>
        private void GenerateMethodReturnCode(StringBuilder builder, TypeSyntax returnType, SourceGenerateContext context)
        {
            if (returnType != null)
            {
                string typeName = returnType.GetTypeName(context.Semantic);
                //builder.Append("\t\t\t").AppendLine($"//返回值类型：{typeName}");
                builder.Append(context.LinePrefix);
                switch (typeName)
                {
                    //  HttpResult，则直接返回
                    case "Snail.Abstractions.Web.DataModels.HttpResult":
                        builder.AppendLine("return hr;");
                        break;
                    //  string，则直接返回HttpResult字符串
                    case "string":
                    case "String":
                        builder.AppendLine($"return await hr.AsStringAsync;");
                        break;
                    //  默认做反序列化：用简版类型名称
                    default:
                        string retTypeName = returnType.ToFullString().Trim();
                        builder.AppendLine($"return await hr.AsAsync<{retTypeName}>();");
                        break;
                }
            }
        }
        #endregion
    }
}
