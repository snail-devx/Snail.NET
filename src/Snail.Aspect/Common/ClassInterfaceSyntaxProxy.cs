using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Attributes;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using static Snail.Aspect.Common.Utils.SyntaxMiddlewareHelper;

namespace Snail.Aspect.Common
{
    /// <summary>
    /// 【类型定义】语法节点代理器 <br />
    ///     1、包含语法节点：ClassDeclarationSyntax、InterfaceDeclarationSyntax、RecordDeclarationSyntax、StructDeclarationSyntax <br />
    ///     2、本代理器仅针对class和interface做代理，为其实现相关功能切面编程；如Http接口自动实现、缓存管理等等<br />
    /// </summary>
    internal class ClassInterfaceSyntaxProxy : ISyntaxProxy
    {
        #region 属性变量
        /// <summary>
        /// 中间件配置
        /// </summary>
        private static readonly IList<Func<TypeDeclarationSyntax, SemanticModel, ITypeDeclarationMiddleware>> _middlewareConfigs;
        /// <summary>
        /// 类型名：<see cref="AspectIgnoreAttribute"/>
        /// </summary>
        protected static readonly string TYPENAME_AspectIgnoreAttribute = typeof(AspectIgnoreAttribute).FullName;
        /// <summary>
        /// 固定需要引入的命名空间几何
        /// </summary>
        protected static readonly IReadOnlyList<string> FixedNamespaces = new List<string>()
        {
            "Snail.Abstractions.Dependency.Attributes",
            "Snail.Abstractions.Dependency.Enumerations",
            "Snail.Aspect.Common.Attributes"
        };

        /// <summary>
        /// 类型定义语法节点
        /// </summary>
        protected readonly TypeDeclarationSyntax Node;
        /// <summary>
        /// 是否是接口
        /// </summary>
        protected readonly bool IsInterface;
        /// <summary>
        /// 语义模型
        /// </summary>
        protected readonly SemanticModel Semantic;
        /// <summary>
        /// 节点所处的命名空间
        /// </summary>
        protected readonly string Namespace;
        /// <summary>
        /// 唯一标记
        /// </summary>
        protected readonly string Key;
        /// <summary>
        /// 实现类的类名称
        /// </summary>
        protected readonly string Class;
        /// <summary>
        /// 源码生成过程中的插件
        /// </summary>
        protected readonly IReadOnlyCollection<ITypeDeclarationMiddleware> Middlewares;
        #endregion

        #region 构造方法
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static ClassInterfaceSyntaxProxy()
        {
            _middlewareConfigs = new List<Func<TypeDeclarationSyntax, SemanticModel, ITypeDeclarationMiddleware>>();
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        private ClassInterfaceSyntaxProxy(TypeDeclarationSyntax node, SemanticModel semantic, List<ITypeDeclarationMiddleware> middlewares)
        {
            Node = node;
            IsInterface = node is InterfaceDeclarationSyntax;
            Semantic = semantic;
            Middlewares = new ReadOnlyCollection<ITypeDeclarationMiddleware>(middlewares);
            //  生成类型的唯一标记：基于命名空间+类型名做md5(截取前10位作为Key)
            {
                Namespace = node.GetNamespace() ?? string.Empty;
                string key = node.TypeParameterList == null
                    ? Namespace.AsMD5().Substring(0, 10)
                    : $"{Namespace}_{node.TypeParameterList}".AsMD5().Substring(0, 10);
                Key = $"{node.Identifier}_{key}";
            }
            //  类名称
            Class = $"{Node.Identifier}_Impl";
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 配置中间件
        /// </summary>
        /// <param name="middleware">中间件构造委托</param>
        public static void Config(Func<TypeDeclarationSyntax, SemanticModel, ITypeDeclarationMiddleware> middleware)
            => _middlewareConfigs.TryAdd(middleware);

        /// <summary>
        /// 构建语法提供程序
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IncrementalValuesProvider<ISyntaxProxy> BuildProvider(IncrementalGeneratorInitializationContext context)
        {
            //  有中间件时，才构建代理
            return context.SyntaxProvider.CreateSyntaxProvider<ISyntaxProxy>(
                predicate: (syntax, _) => syntax is InterfaceDeclarationSyntax || syntax is ClassDeclarationSyntax,
                transform: (ctx, _) =>
                {
                    TypeDeclarationSyntax tds = ctx.Node as TypeDeclarationSyntax;
                    //  如果标记为AspectIgnoreAttribute，则忽略掉
                    AttributeSyntax attr = tds.AttributeLists.GetAttribute(ctx.SemanticModel, TYPENAME_AspectIgnoreAttribute);
                    if (attr != null)
                    {
                        return null;
                    }
                    //  基于中间件，构建代码分析代理器
                    List<ITypeDeclarationMiddleware> middlewares = new List<ITypeDeclarationMiddleware>();
                    foreach (var item in _middlewareConfigs)
                    {
                        ITypeDeclarationMiddleware middleware = item.Invoke(tds, ctx.SemanticModel);
                        middlewares.TryAdd(middleware);
                    }
                    return middlewares.Count > 0
                        ? new ClassInterfaceSyntaxProxy(tds, ctx.SemanticModel, middlewares)
                        : null;
                }
            );
        }
        #endregion

        #region ISyntaxProxy
        /// <summary>
        /// 唯一Key值，将作为生成的源码cs文件名称 <br />
        /// </summary>
        /// <remarks>若返回null，则不会生成cs文件</remarks>
        string ISyntaxProxy.Key => Key;

        /// <summary>
        /// 生成HTTP接口实现类源码
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns>生成好的代码</returns>
        string ISyntaxProxy.GenerateCode(SourceProductionContext ctx)
        {
            StringBuilder builder = new StringBuilder();
            SourceGenerateContext context = new SourceGenerateContext(Node, ctx, Semantic);
            //  准备生成：执行插件的PrepareGenerate方法
            foreach (var mi in Middlewares)
            {
                mi.PrepareGenerate(context);
            }
            //  命名空间+类声明+构造方法
            {
                builder.AppendLine($"namespace {context.DefaultNamespace}.{Namespace.Replace('.', '_')};");
                GenerateClassDeclarationCode(builder, context)
                       .AppendLine("{");
            }
            //  构造方法；有才生成，忽略private标记的、忽略static的
            GenerateConstructor(builder, context);
            //  遍历重写的方法代码
            {
                builder.Append('\t').AppendLine($"#region 重写{Node.Identifier}方法");
                foreach (MethodDeclarationSyntax mNode in Node.ChildNodes().OfType<MethodDeclarationSyntax>())
                {
                    GenerateMethod(builder, mNode, context);
                }
                builder.Append('\t').AppendLine("#endregion");
            }
            //  合并辅助代码
            {
                context.Reset("\t");
                builder.AppendLine().Append('\t').AppendLine("#region 辅助代码");
                foreach (var middleware in Middlewares)
                {
                    string code = middleware.GenerateAssistantCode(context)?.TrimEnd();
                    if (string.IsNullOrEmpty(code) == false)
                    {
                        builder.AppendLine(code);
                    }
                }
                //  生成依赖注入字段的非null验证方法；方法名称 AspectRequiredFieldValidate，通过[Inject]属性标记
                if (context.HasRequiredFields() == true)
                {
                    builder.Append('\t').AppendLine("//   依赖注入的必填字段验证")
                           .Append('\t').AppendLine("[Inject]")
                           .Append('\t').AppendLine("private void AspectRequiredFieldValidate()")
                           .Append('\t').AppendLine("{");
                    context.ForEachRequiredFields(kv =>
                    {
                        builder.Append("\t\t").AppendLine($"ThrowIfNull({kv.Key} ,\"{kv.Value}\");");
                    });
                    builder.Append('\t').AppendLine("}");
                }
                builder.Append('\t').AppendLine("#endregion");
            }
            //  组装代码：using 命名空间；插入到最前面
            //      using处理：插入到最前面；using命名空间，只有再最后的时候才知道要引入哪些
            builder.Insert(0, "\r\n").Insert(0, "\r\n")
                   .Insert(0, string.Join("\r\n", context.Namespaces.Distinct().OrderBy(item => item).Select(item => $"using {item};")))
                   .Insert(0, "#pragma warning disable CS1591, CS8600, CS8602, CS8603, CS8604\r\n")
                   .Insert(0, "#nullable enable\r\n");
            //      类和命名空间收尾，追加到最后面：
            builder.AppendLine("}")
                   .AppendLine("#pragma warning restore CS1591, CS8600, CS8602, CS8603, CS8604")
                   .AppendLine("#nullable disable");

            return builder.ToString();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 生成类的声明代码：类上的特性标签+类名（访问修饰符、继承、实现等）
        /// </summary>
        /// <returns></returns>
        private StringBuilder GenerateClassDeclarationCode(StringBuilder builder, SourceGenerateContext context)
        {
            //  准备工作：类需要的固定命名空间处理；基类型整理
            context.AddNamespaces(FixedNamespaces)
                   .AddNamespaces(Node.GetUsedNamespaces())
                   .AddNamespaces(Node.AttributeLists.GetUsedNamespaces(context.Semantic));
            string baseName = $"{Node.Identifier}{Node.TypeParameterList}";
            //  加入特性标签
            {
                //  强制加上aspct和继承实现类型组件注入生命
                builder.AppendLine($"[{nameof(AspectAttribute).Replace("Attribute", "")}]");
                string tmpCode = null;
                if (Node.TypeParameterList?.Parameters.Count > 0)
                {
                    tmpCode = string.Join(",", Node.TypeParameterList.Parameters.Select(p => string.Empty));
                    tmpCode = $"[Component(Lifetime = LifetimeType.Singleton, From = typeof({Node.Identifier}<{tmpCode}>))]";
                }
                tmpCode = tmpCode ?? $"[Component<{baseName}>(Lifetime = LifetimeType.Singleton)]";
                builder.AppendLine(tmpCode);
                //  其他特性标签的处理，去掉[Component]
                foreach (var attr in Node.AttributeLists.GetAttributes())
                {
                    string attrName = $"[{attr}]";
                    if (attrName != "[Component]")
                    {
                        builder.AppendLine(attrName);
                    }
                }
            }
            //  类声明，强制加入sealed扩展，并移除一些不然abstract生命
            {
                foreach (var token in Node.Modifiers)
                {
                    switch (token.Kind())
                    {
                        case SyntaxKind.SealedKeyword:
                        case SyntaxKind.AbstractKeyword:
                            break;
                        default:
                            builder.Append(token).Append(" ");
                            break;
                    }
                }
                builder.Append("sealed ")
                       .Append("class ").Append(Class)
                       .Append(Node.TypeParameterList).Append(" : ").AppendLine(baseName);
            }

            return builder;
        }

        /// <summary>
        /// 生成构造方法
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private void GenerateConstructor(StringBuilder builder, SourceGenerateContext context)
        {
            context.LinePrefix = "\t";
            //有才生成，忽略private标记的、忽略static的
            var nodes = Node.ChildNodes().OfType<ConstructorDeclarationSyntax>();
            if (nodes.Any() == true)
            {
                builder.Append(context.LinePrefix).AppendLine("#region 构造方法");
                foreach (var cNode in nodes)
                {
                    bool bValue = cNode.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.StaticKeyword));
                    if (bValue == true)
                    {
                        continue;
                    }
                    //  保留属性申明
                    GenerateCodeByAttribute(builder, cNode.AttributeLists, context);
                    //  方法名称相关申明
                    //      访问修饰符、方法名称、参数信息
                    builder.Append(context.LinePrefix)
                           .Append($"{cNode.Modifiers}").Append(cNode.Modifiers.Count > 0 ? " " : string.Empty)
                           .Append(Class);
                    var pNames = GenerateCodeByParameter(builder, cNode.ParameterList, context);
                    //      base访问基类 + 空方法实现
                    builder.Append(context.LinePrefix).AppendLine($": base({string.Join(", ", pNames)})");
                    builder.Append(context.LinePrefix).AppendLine("{ }");
                }
                builder.Append(context.LinePrefix).AppendLine("#endregion")
                       .AppendLine();
            }
        }

        /// <summary>
        /// 生成方法实现代码
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="mNode"></param>
        /// <param name="context"></param>
        /// <returns>是成功生成代码，返回true；否则返回false</returns>
        private bool GenerateMethod(StringBuilder builder, MethodDeclarationSyntax mNode, SourceGenerateContext context)
        {
            //      显式指定了Ignore，则忽略
            if (mNode.AttributeLists.GetAttribute(context.Semantic, TYPENAME_AspectIgnoreAttribute) != null)
            {
                return false;
            }
            //  执行【中间件】生成具体代码；未生成代码，直接返回不生成
            MethodGenerateOptions options = new MethodGenerateOptions(mNode, context);
            string code = GenerateMethodByRunMiddleware(mNode, context, options);
            if (string.IsNullOrEmpty(code) == true)
            {
                return false;
            }
            //  生成方法：则合并方法体，必须不为private、类方法必须为virtual，否则 给出错误警告，不生成
            //      1、生成方法代码：保留属性信息，并分析命名空间
            GenerateCodeByAttribute(builder, mNode.AttributeLists, context);
            //      2、返回值、修饰符：接口忽略访问修饰符；方法追加override   示例 public async override string
            builder.Append('\t')
                   .Append(IsInterface == false && options.AccessTokens.Count > 0
                        ? $"{string.Join(" ", options.AccessTokens)} "
                        : string.Empty
                   )
                   .Append(options.IsAsync ? "async " : string.Empty)
                   .Append(IsInterface ? string.Empty : "override ")
                   .Append($"{mNode.ReturnType} ");
            //      3、方法名称声明：若为接口，则显示声明
            builder.Append(IsInterface ? $"{Node.Identifier}{Node.TypeParameterList}." : string.Empty)
                   .Append($"{mNode.Identifier}");
            //      4、方法参数：保留参数属性标记；分析参数和参数属性用到的命名空间 示例：(string x,LockList<string>? x2,[HttpBody, Inject]string xx1}
            _ = GenerateCodeByParameter(builder, mNode.ParameterList, context);
            //      5、合并方法实现代码：根据需要生成【切面方法参数映射字段】信息
            builder.Append('\t').AppendLine("{");
            if (context.NeedMethodParameterMap == true)
            {
                builder.Append(context.LinePrefix).Append($"var {context.GetMethodParameterMapName(mNode)} = ");
                if (mNode.ParameterList.Parameters.Count > 0)
                {
                    string tmpCode = string.Join(
                        ",",
                        mNode.ParameterList.Parameters.Select(p => $"{{ \"{p.Identifier.Text}\", {p.Identifier.Text} }}")
                    );
                    builder.AppendLine($"new Dictionary<string, object?>() {{ {tmpCode} }};");
                }
                else
                {
                    builder.AppendLine("null;");
                }
            }
            builder.Append(context.LocalMethods)
                   .AppendLine(code.TrimEnd());
            builder.Append('\t').AppendLine("}");

            return true;
        }
        /// <summary>
        /// 运行插件生成方法代码
        /// </summary>
        /// <param name="mNode"></param>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GenerateMethodByRunMiddleware(MethodDeclarationSyntax mNode, SourceGenerateContext context, MethodGenerateOptions options)
        {
            //  1、构建方法中间件：起始中间件，接口和抽象方法无需生成；否则【基类方法调用】作为起始中间件
            MethodCodeDelegate mcd = (IsInterface || options.IsAbstract) ? null : CallBaseMethodCode;
            foreach (var item in Middlewares.Reverse())
            {
                mcd = BuildDelegate(item, mcd);
            }
            //  2、执行代码生成，执行前上下文信息重置
            context.Reset(linePrefix: TAB_MethodSpace)
                   .AddVarNames(mNode.ParameterList.Parameters.Select(item => item.Identifier.Text).ToArray());
            string code = mcd.Invoke(mNode, context, options);
            //  3、校正生成情况：根据生成情况，判断是否合法
            //      未生成：abstract方法必须有实现、接口方法若无实现则需要报错
            if (context.Generated == false)
            {
                context.ReportErrorIf(
                    condition: options.IsAbstract,
                    message: "abstract方法无[XXXMethod]标记，Aspect无法进行自动实现",
                    syntax: mNode
                );
                context.ReportErrorIf(
                    condition: IsInterface && mNode.HasImplemented() == false,
                    message: "接口方法无[XXXMethod]标记且无默认实现，Aspect无法进行自动实现",
                    syntax: mNode
                );
                return null;
            }
            //      已生成：无代码，说明生成有问题，无需操作直接返回
            if (string.IsNullOrEmpty(code) == true)
            {
                return null;
            }
            //      已生成：显示接口实现方法：忽略
            if (options.ExplicitInterface == true)
            {
                context.ReportWarning($"显示实现接口的方法[{mNode.Identifier}]，将忽略Aspect相关实现", mNode);
                return null;
            }
            //      已生成： 类方法，非virtual/abstract：给出错误警告：忽略
            if (IsInterface == false && options.IsVirtual == false && options.IsAbstract == false)
            {
                context.ReportError(message: "类方法无法重写实现，除非标记为virtual/abstract", mNode);
                return null;
            }
            //      已生成：分析可用的访问修饰符；则判断方法是否能够进行【切面编程】，如private、static方法不能重写，无法进行切面逻辑
            if (options.IsPrivate || options.IsSealed || options.IsStatic)
            {
                context.ReportError
                (
                    message: "private/static/sealed方法，无法进行重写实现",
                    syntax: mNode
                );
                return null;
            }
            //  4、整理代码返回：对方法内部的辅助代码，做优化，和实际代码code之间空行
            if (context.LocalMethods.Length > 0)
            {
                context.LocalMethods.AppendLine();
            }
            return code;
        }
        /// <summary>
        /// 构建方法代码生成委托
        /// </summary>
        /// <param name="middleware"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private static MethodCodeDelegate BuildDelegate(ITypeDeclarationMiddleware middleware, MethodCodeDelegate next)
             => (method, context, options) => middleware.GenerateMethodCode(method, context, options, next);

        /// <summary>
        /// 基于属性节点生成代码
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="attributeList"></param>
        /// <param name="context"></param>
        private static void GenerateCodeByAttribute(StringBuilder builder, SyntaxList<AttributeListSyntax> attributeList, SourceGenerateContext context)
        {
            //  后期考虑，针对一些标记性的属性不用写进来，如http的HtppMethod等，就是用于代码生成的，加进来没有意义
            foreach (var attr in attributeList.GetAttributes())
            {
                context.AddNamespaces(attr.GetUsedNamespaces(context.Semantic));
                builder.Append('\t').AppendLine($"[{attr}]");
            }
        }
        /// <summary>
        /// 基于参数生成代码<br />
        ///     1、保留参数Attribute、in、out等信息<br />
        ///     2、生成参数代码格式：(参数类型 参数名称, [参数属性]参数类型 参数名称...)
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="parameters"></param>
        /// <param name="context"></param>
        /// <returns>参数名称</returns>
        private static List<string> GenerateCodeByParameter(StringBuilder builder, ParameterListSyntax parameters, SourceGenerateContext context)
        {
            List<string> names = new List<string>();
            builder.Append("(")
                   .Append(string.Join(", ", parameters.Parameters.Select(parameter =>
                   {
                       names.Add(parameter.Identifier.Text);
                       context.AddNamespaces(parameter.Type?.GetUsedNamespaces(context.Semantic));
                       context.AddNamespaces(parameter.AttributeLists.GetUsedNamespaces(context.Semantic));
                       //  先忽略方法参数的访问修饰符，如params in out参数等；在需要进行中间件生成代码时，内部会用到本地方法等，in、out参数无效；重写方法 params无效去掉
                       //return parameter.Modifiers.Count > 0
                       //    ? $"{parameter.Modifiers} {parameter.AttributeLists}{parameter.Type} {parameter.Identifier}"
                       //    : $"{parameter.AttributeLists}{parameter.Type} {parameter.Identifier}";

                       string tmpCode = parameter.AttributeLists.Count > 0
                           ? $"{parameter.AttributeLists} "
                           : string.Empty;
                       return $"{tmpCode}{parameter.Type} {parameter.Identifier}";
                   })))
                   .AppendLine(")");
            return names;
        }
        #endregion
    }
}
