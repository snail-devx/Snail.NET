using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;

namespace Snail.Aspect.Common.Components
{
    /// <summary>
    /// 源码生成的上下文；封装<see cref="SourceProductionContext"/>对外提供部分能力
    /// </summary>
    internal class SourceGenerateContext
    {
        #region 属性变量
        /// <summary>
        /// 默认命名空间；有需要在此命名空间下追加
        /// </summary>
        public readonly string DefaultNamespace = "_AspectSource_";
        /// <summary>
        /// roslyn提供的实际上下文
        /// </summary>
        public readonly SourceProductionContext Context;
        /// <summary>
        /// 语义模型对象，用于获取语法节点Symbol
        /// </summary>
        public readonly SemanticModel Semantic;
        /// <summary>
        /// 命名空间，收集源码生成时用到过的命名空间；在生成实现类时using引入
        /// </summary>
        public readonly List<string> Namespaces = new List<string>();

        /// <summary>
        /// 所属类型，如class和Interface语法节点对象
        /// </summary>
        public readonly TypeDeclarationSyntax TypeSyntax;
        /// <summary>
        /// 类型是否是class
        /// </summary>
        public bool TypeIsClass => TypeSyntax.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);

        /// <summary>
        /// 本地方法代码
        /// </summary>
        public StringBuilder LocalMethods { get; } = new StringBuilder();

        /// <summary>
        /// 代码行前缀，指定每行代码的缩进；生成格式化代码用
        /// </summary>
        public string LinePrefix { set; get; } = string.Empty;

        /// <summary>
        /// 是否生成了源码；<see cref="ITypeDeclarationMiddleware"/>中若生成了自己的代码，设置为true
        /// </summary>
        public bool Generated { set; get; } = false;
        /// <summary>
        /// 实际生成过代码的中间件编码；在生成代码返回前添加
        /// </summary>
        public IList<string> Middlewares = new List<string>();

        /// <summary>
        /// 生成过程中用到的参数名称
        /// </summary>
        private readonly List<string> _varNames = new List<string>();
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public SourceGenerateContext(TypeDeclarationSyntax typeSyntax, SourceProductionContext context, SemanticModel semantic)
        {
            TypeSyntax = typeSyntax;
            Context = context;
            Semantic = semantic;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 重置上下文
        /// </summary>
        /// <param name="linePrefix">代码行前缀，指定每行代码的缩进</param>
        /// <returns></returns>
        public SourceGenerateContext Reset(string linePrefix)
        {
            LinePrefix = linePrefix;
            LocalMethods.Clear();
            Middlewares.Clear();
            _varNames.Clear();

            return this;
        }
        /// <summary>
        /// 添加使用到的命名空间
        /// </summary>
        /// <param name="nss"></param>
        /// <returns></returns>
        public SourceGenerateContext AddNamespaces(params string[] nss)
        {
            Namespaces.TryAddRange(nss);
            return this;
        }
        /// <summary>
        /// 添加使用到的命名空间
        /// </summary>
        /// <param name="nss"></param>
        /// <returns></returns>
        public SourceGenerateContext AddNamespaces(IEnumerable<string> nss)
        {
            Namespaces.TryAddRange(nss);
            return this;
        }
        /// <summary>
        /// 添加实际生成过代码的中间件编码 <br/>
        ///     1、仅执行next中间件生成代码，不作为实际生成过代码
        /// </summary>
        /// <param name="code"></param>
        public void AddGeneratedMiddleware(string code)
        {
            Middlewares.Add(code);
        }

        /// <summary>
        /// 添加已有变量名称信息
        /// </summary>
        /// <param name="vars"></param>
        public void AddVarNames(params string[] vars)
        {
            _varNames.TryAddRange(vars);
        }
        /// <summary>
        /// 获取变量名称<br />
        ///      1、若<paramref name="varName"/>已经使用过了，则重新生成一个<br />
        ///      2、避免代码中参数名称重复，特别是和方法、类定义的变量重复
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        public string GetVarName(string varName)
        {
            int index = 1;
            while (_varNames.IndexOf(varName) != -1)
            {
                varName = $"{varName}{index}";
                index += 1;
            }
            return varName;
        }

        /// <summary>
        /// 报告【错误】诊断信息
        /// </summary>
        /// <param name="message">诊断消息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        public void ReportError(string message, SyntaxNode syntax)
            => ReportDiagnostic(id: "Snail_Error", message, DiagnosticSeverity.Error, syntax);
        /// <summary>
        /// 报告【错误】诊断信息；在<paramref name="condition"/>条件为true时执行
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message">诊断消息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        public void ReportErrorIf(bool condition, string message, SyntaxNode syntax)
        {
            _ = condition && ReportDiagnostic(id: "Snail_Error", message, DiagnosticSeverity.Error, syntax);
        }

        /// <summary>
        /// 报告【警告】诊断信息
        /// </summary>
        /// <param name="message">诊断消息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        public void ReportWarning(string message, SyntaxNode syntax)
            => ReportDiagnostic(id: "Snail_Warning", message, DiagnosticSeverity.Warning, syntax);

        /// <summary>
        /// 报告【信息】诊断信息
        /// </summary>
        /// <param name="message">诊断消息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        public void ReportInfo(string message, SyntaxNode syntax)
            => ReportDiagnostic(id: "Snail_Info", message, DiagnosticSeverity.Info, syntax);
        /// <summary>
        /// 报告【信息】诊断信息；在<paramref name="condition"/>条件为true时执行
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message">诊断消息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        public void ReportInfoIf(bool condition, string message, SyntaxNode syntax)
        {
            _ = condition && ReportDiagnostic(id: "Snail_Info", message, DiagnosticSeverity.Info, syntax);
        }

        /// <summary>
        /// 报告诊断信息
        /// </summary>
        /// <param name="id">诊断信息id</param>
        /// <param name="message">诊断消息</param>
        /// <param name="severity">级别：错误、警告、信息</param>
        /// <param name="syntax">语法节点对象；用户取location等信息</param>
        /// <returns></returns>
        public bool ReportDiagnostic(string id, string message, DiagnosticSeverity severity, SyntaxNode syntax)
        {
            var diagnostic = Diagnostic.Create(
                descriptor: new DiagnosticDescriptor(
                    id,
                    title: "Snail_Aspect",
                    messageFormat: message,
                    category: "Usage",
                    defaultSeverity: severity,
                    isEnabledByDefault: true
                ),
                location: syntax?.GetLocation()
            );
            Context.ReportDiagnostic(diagnostic);

            return true;
        }
        #endregion
    }
}
