using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.DataModels;
using Snail.Aspect.Common.Delegates;
using Snail.Aspect.Common.Extensions;
using Snail.Aspect.Common.Interfaces;
using Snail.Aspect.General.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snail.Aspect.General;

/// <summary>
/// 【Validate】语法中间
/// <para>1、侦测打了<see cref="ValidateAspectAttribute"/>标签的interface、class，为其生成实现class，并注册为组 </para>
/// <para>2、自动为方法加入【验证逻辑】，如验证方法参数有效性，字段属性有效性 </para>
/// </summary>
internal class ValidateSyntaxMiddleware : ITypeDeclarationMiddleware
{
    #region 属性变量
    /// <summary>
    /// 类型名：<see cref="ValidateAspectAttribute"/>
    /// </summary>
    protected static readonly string TYPENAME_ValidateAspectAttribute = typeof(ValidateAspectAttribute).FullName!;
    /// <summary>
    /// 类型名：<see cref="RequiredAttribute"/>
    /// </summary>
    protected const string TYPENAME_RequiredAttribute = "Snail.Aspect.General.Attributes.RequiredAttribute";
    /// <summary>
    /// 类型名：<see cref="RequiredAttribute"/>
    /// </summary>
    protected const string TYPENAME_AnyAttribute = "Snail.Aspect.General.Attributes.AnyAttribute";

    /// <summary>
    /// 固定需要引入的命名空间集合
    /// </summary>
    protected static readonly IReadOnlyList<string> FixedNamespaces =
    [
        //  全局依赖的
        //       这几个为了方便内部判断，如IsNullOrEmpty
        "static Snail.Utilities.Common.Utils.ArrayHelper",
        "static Snail.Utilities.Common.Utils.ObjectHelper",
        "static Snail.Utilities.Common.Utils.StringHelper",
        "static Snail.Utilities.Collections.Utils.DictionaryHelper",
        "static Snail.Utilities.Collections.Utils.ListHelper",
        //  验证 切面编程相关命名空间
        "Snail.Utilities.Common.Interfaces",
        typeof(ValidateAspectAttribute).Namespace!,
        typeof(RequiredAttribute).Namespace!,
    ];

    /// <summary>
    /// 是否需要【辅助】代码
    /// </summary>
    private bool _needAssistantCode = false;
    #endregion

    #region 构造方法
    /// <summary>
    /// 私有构造方法
    /// </summary>
    private ValidateSyntaxMiddleware()
    {
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
        //  仅针对“有【ValidateAspectAttribute】属性标记的interface和class”做处理
        if (node is InterfaceDeclarationSyntax || node is ClassDeclarationSyntax)
        {
            AttributeSyntax? attr = node.AttributeLists.GetAttribute(semantic, TYPENAME_ValidateAspectAttribute);
            return attr != null
                ? new ValidateSyntaxMiddleware()
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
        //  判断是否有验证参数，有则生成
        StringBuilder builder = new();
        foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
        {
            //  生成验证代码
            bool bValue = false;
            string parameterName = parameter.Identifier.Text;
            ITypeSymbol type = context.Semantic.GetTypeInfo(parameter.Type!).Type!;
            bool hasRequired = false;
            //      属性验证
            foreach (var attr in parameter.AttributeLists.GetAttributes())
            {
                switch ($"{context.Semantic.GetSymbolInfo(attr).Symbol!.ContainingType}")
                {
                    //  Snail.Aspect.General.Attributes.RequiredAttribute
                    case TYPENAME_RequiredAttribute:
                        GenerateRequiredValidateCode(builder, context, parameter, type, attr);
                        hasRequired = true;
                        break;
                }
            }
            //      IValidatable自定义验证
            if (type.IsIValidatable() == true)
            {
                string ivMsgVar = context.GetVarName("ivMsg");
                builder.Append(context.LinePrefix).AppendLine(hasRequired
                    ? $"if (((IValidatable){parameterName}).Validate(out string? {ivMsgVar}) == false)"
                    : $"if ({parameterName} != null && ((IValidatable){parameterName}).Validate(out string? {ivMsgVar}) == false)"
                );
                builder.Append(context.LinePrefix).AppendLine("{")
                       .Append(context.LinePrefix).Append('\t').AppendLine($"throw new ArgumentException($\"{parameterName}验证失败：{{{ivMsgVar}}}\");")
                       .Append(context.LinePrefix).AppendLine("}");
                bValue = true;
            }
            // bValue = GenerateValidateCodeByAttribute(builder, context, parameter, type);
            // bValue = GenerateValidateCodeByIValidatable(builder, context, parameter, type) || bValue;
            //  验证是否可生成验证代码：如out参数
            context.ReportErrorIf(
                bValue == true && parameter.Modifiers.Any(SyntaxKind.OutKeyword) == true,
                "不支持为out参数生成验证代码",
                parameter
            );
        }
        if (builder.Length > 0)
        {
            _needAssistantCode = true;
            context.Generated = true;
        }
        // context.ReportError($"{builder}", method);
        //  执行next逻辑，合并代码返回
        string? nextCode = next?.Invoke(method, context, options);
        if (nextCode?.Length > 0)
        {
            builder.Append(nextCode);
        }
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
        if (_needAssistantCode == true)
        {
            context.AddNamespaces(FixedNamespaces);
        }
        return null;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 生成【Required】验证代码
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="context"></param>
    /// <param name="parameter"></param>
    /// <param name="parameterType"></param>
    /// <param name="requiredAttr"></param>
    /// <returns></returns>
    private static bool GenerateRequiredValidateCode(StringBuilder builder, SourceGenerateContext context, ParameterSyntax parameter, ITypeSymbol parameterType, AttributeSyntax requiredAttr)
    {
        bool canValidateNull = parameterType.TypeKind == TypeKind.Class || parameterType.TypeKind == TypeKind.Interface
            || parameterType.IsArray(out _) || parameterType.IsNullable();
        bool canValidateEmpty = parameterType.IsString() || parameterType.IsArray(out _)
            || parameterType.IsList(out _) || parameterType.IsDictionary(out _, out _);
        if (canValidateEmpty == false && canValidateNull == false)
        {
            context.ReportError("[Required]仅支持class/interface/Array/nullable<T>类型参数", parameter);
            return false;
        }
        //  生成代码
        string validateFunc = canValidateEmpty == true ? "ThrowIfNullOrEmpty" : "ThrowIfNull";
        var mag = requiredAttr.GetArguments().FirstOrDefault();
        builder.Append(context.LinePrefix).AppendLine(mag == null
            ? $"{validateFunc}({parameter.Identifier});"
            : $"{validateFunc}({parameter.Identifier}, {mag});"
        );
        return true;
    }
    #endregion
}