using Microsoft.CodeAnalysis;

namespace Snail.Aspect.Common.Extensions;

/// <summary>
/// 源码生成相关上下文扩展方法
/// </summary>
internal static class ContextExtensions
{
    #region 公共方法

    #region SourceProductionContext
    /// <summary>
    /// 报告诊断信息
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id">诊断信息id</param>
    /// <param name="message">诊断消息</param>
    /// <param name="severity">级别：错误、警告、信息</param>
    /// <param name="syntax">语法节点对象；用户取location等信息</param>
    public static void ReportDiagnostic(this SourceProductionContext context, string id, string message, DiagnosticSeverity severity, SyntaxNode syntax)
    {
        var diagnostic = Diagnostic.Create(
            descriptor: new DiagnosticDescriptor(
                id,
                title: "Snail.CodeAnalysis.Diagnostic",
                messageFormat: message,
                category: "Usage",
                defaultSeverity: severity,
                isEnabledByDefault: true
            ),
            location: syntax?.GetLocation()
        );
        context.ReportDiagnostic(diagnostic);
    }
    /// <summary>
    /// 报告诊断信息；在<paramref name="condition"/>条件为true时执行
    /// </summary>
    /// <param name="context"></param>
    /// <param name="condition"></param>
    /// <param name="id">诊断信息id；这边编码值无所谓，随便取能区分即可</param>
    /// <param name="message">诊断消息</param>
    /// <param name="severity">级别：错误、警告、信息</param>
    /// <param name="syntax">语法节点对象；用户取location等信息</param>
    public static void ReportDiagnosticIf(this SourceProductionContext context, bool condition, string id, string message, DiagnosticSeverity severity, SyntaxNode syntax)
    {
        if (condition == true)
        {
            context.ReportDiagnostic(id, message, severity, syntax);
        }
    }
    #endregion

    #endregion
}
