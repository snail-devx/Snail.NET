using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;

namespace Snail.Aspect.Common
{
    /// <summary>
    /// 通用的语法节点源码中间件 <br />
    ///     1、实现<see cref="IMethodRunHandle"/>的类型，自动对方法做重写，然后在方法执行前后，执行句柄方法 <br />
    ///     2、暂时不支持打属性标签的方式，实现通用的面向切面编程逻辑；涉及基类等太麻烦； <br />
    /// </summary>
    internal class GeneralSyntaxMiddleware : ITypeDeclarationMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 名称：本地方法名称
        /// </summary>
        protected const string NAME_LocalMethod = "_AspectNextCodeMethod";
        /// <summary>
        /// 类型名：<see cref="IMethodRunHandle"/>
        /// </summary>
        protected static readonly string TYPENAME_IMethodHandle = typeof(IMethodRunHandle).FullName;
        /// <summary>
        /// 固定需要引入的命名空间集合
        /// </summary>
        protected static readonly IReadOnlyList<string> FixedNamespaces = new List<string>()
        {
            //  全局依赖的
            typeof(Task).Namespace,//                           System
            "Snail.Utilities.Common",//                         
            "Snail.Utilities.Common.Utils",//                   typeof(ObjectHelper).Namespace,           
            "Snail.Utilities.Collections.Utils",//              typeof(ListHelper).Namespace,//
            "Snail.Utilities.Common.Extensions",
            "Snail.Utilities.Collections.Extensions",
            //  切面编程相关命名空间
            typeof(IMethodRunHandle).Namespace,
            typeof(MethodRunHandleExtensions).Namespace,
            typeof(MethodRunContext).Namespace,
        };
        /// <summary>
        /// 是否需要【辅助】代码
        /// </summary>
        private bool _needAssistantCode = false;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        private GeneralSyntaxMiddleware() { }
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
            //  仅针对实现了 IAspectMethodHandle 接口的class；interface继承IAspectMethodHandle，此时做注入没有意义，不做处理
            if (node is ClassDeclarationSyntax)
            {
                bool? bValue = semantic.GetDeclaredSymbol(node).AllInterfaces.Any(ti => $"{ti}" == TYPENAME_IMethodHandle);
                return bValue == true ? new GeneralSyntaxMiddleware() : null;
            }
            return null;
        }
        #endregion

        #region ITypeDeclarationMiddleware
        /// <summary>
        /// 准备生成：做一下信息初始化，或者将将一些信息加入上下文
        /// </summary>
        /// <param name="context"></param>
        void ITypeDeclarationMiddleware.PrepareGenerate(SourceGenerateContext context) { }

        /// <summary>
        /// 生成方法代码；仅包括方法内部代码
        /// </summary>
        /// <param name="method">方法语法节点</param>
        /// <param name="context">上下文对象</param>
        /// <param name="next">下一步操作；若为null则不用继续执行，返回即可</param>
        /// <param name="options">方法生成配置选项</param>
        /// <remarks>若不符合自身业务逻辑</remarks>
        /// <returns>代码字符串</returns>
        string ITypeDeclarationMiddleware.GenerateMethodCode(MethodDeclarationSyntax method, SourceGenerateContext context, MethodGenerateOptions options, MethodCodeDelegate next)
        {
            //  只要有下一步的具体实现，都需要进行具体实现
            string nextRunCode = GenerateRunCodeWithNext(method, context, options, next, NAME_LocalMethod, simpleBaseCall: false);
            if (string.IsNullOrEmpty(nextRunCode) == true)
            {
                return null;
            }
            //  生成面向切面的相关代码
            StringBuilder builder = new StringBuilder();
            context.Generated = true;
            _needAssistantCode = true;
            string tmpCode = null;
            //      初始化context
            {
                tmpCode = method.ParameterList.Parameters.Count > 0
                    ? string.Join(", ", method.ParameterList.Parameters.Select(p => $"{{ \"{p.Identifier.Text}\", {p.Identifier.Text} }}"))
                    : null;
                builder.Append(context.LinePrefix)
                       .Append($"{nameof(MethodRunContext)} mrhContext = new(")
                       .Append($"\"{method.Identifier}\", ")
                       .Append(tmpCode == null ? "null" : $"new Dictionary<string, object?>() {{ {tmpCode} }}")
                       .AppendLine(");");
            }
            //      生成执行代码：需要区分是否有返回值；配合 IMethodRunHandle 扩展方法，简化代码逻辑；替换下面的旧代码
            {
                tmpCode = nameof(IMethodRunHandle);
                builder.Append(context.LinePrefix)
                       .Append(options.ReturnType == null ? string.Empty : $"return ")
                       .Append(options.IsAsync ? $"await (({tmpCode})this).OnRunAsync" : $"(({tmpCode})this).OnRun")
                       .Append(options.ReturnType == null ? string.Empty : $"<{options.ReturnType}>")
                       .Append('(').Append(NAME_LocalMethod).AppendLine(", mrhContext);");
            }
            /*  旧代码备份：比较冗余，上面简化代码配合 IMethodRunHandle的扩展方法，简化源码生成逻辑
             //      生成next执行委托：基于异步做区分，将结果写入aspectNextData
             {
                 tmpCode = options.IsAsync ? "async Task _AspectRunMethod()" : "void _AspectRunMethod()";
                 if (options.ReturnType != null)
                 {
                     builder.Append(context.LinePrefix).AppendLine(tmpCode)
                            .Append(context.LinePrefix).AppendLine("{")
                            .Append(context.LinePrefix).Append('\t').AppendLine($"{options.ReturnType} aspectNextData = {nextRunCode}")
                            .Append(context.LinePrefix).Append('\t').AppendLine($"mrhContext.SetReturnValue(aspectNextData);")
                            .Append(context.LinePrefix).AppendLine("}");
                 }
                 else
                 {
                     builder.Append(context.LinePrefix).AppendLine(tmpCode)
                            .Append(context.LinePrefix).AppendLine("{")
                            .Append(context.LinePrefix).Append('\t').AppendLine(nextRunCode)
                            .Append(context.LinePrefix).AppendLine("}");
                 }
             }
             //      执行next逻辑
             {
                 tmpCode = options.IsAsync
                     ? $"await (({nameof(IMethodRunHandle)})this).OnRunAsync(_AspectRunMethod, mrhContext);"
                     : $"(({nameof(IMethodRunHandle)})this).OnRun( _AspectRunMethod, mrhContext);";
                 builder.Append(context.LinePrefix).AppendLine(tmpCode);
             }
             //      返回数据
             if (options.ReturnType != null)
             {
                 builder.Append(context.LinePrefix).AppendLine($"return ({options.ReturnType})mrhContext.ReturnValue;");
             }
             */

            context.AddGeneratedMiddleware(nameof(IMethodRunHandle));
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
            if (_needAssistantCode == true)
            {
                context.AddNamespaces(FixedNamespaces);
            }
            return null;
        }
        #endregion
    }
}
