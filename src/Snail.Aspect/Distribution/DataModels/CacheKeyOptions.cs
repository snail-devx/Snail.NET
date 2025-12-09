using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.Extensions;

namespace Snail.Aspect.Distribution.DataModels;

/// <summary>
/// [CacheKey]配置选项
/// </summary>
internal readonly struct CacheKeyOptions
{
    #region 属性变量
    /// <summary>
    /// key是否有效
    /// </summary>
    public bool IsValid { get; }
    /// <summary>
    /// key的变量名称
    /// </summary>
    public string VarName { get; }

    /// <summary>
    /// key是多选项类型；如IList{string}；为false时则是string
    /// </summary>
    public bool IsMulti { get; }
    /// <summary>
    /// key是list集合
    /// </summary>
    public bool IsList { get; }
    /// <summary>
    /// key是数组
    /// </summary>
    public bool IsArray { get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="context"></param>
    public CacheKeyOptions(ParameterSyntax parameter, SourceGenerateContext context)
    {
        //  初始化
        {
            IsValid = false;
            VarName = parameter.Identifier.Text;
            IsMulti = false;
            IsArray = false;
            IsList = false;
        }
        //  分析赋值
        {
            ITypeSymbol type = context.Semantic.GetTypeInfo(parameter.Type).Type;
            if (type.IsArray(out ITypeSymbol realType) == true)
            {
                IsArray = true;
            }
            else if (type.IsList(out realType, inherit: true) == true)
            {
                IsList = true;
            }
            IsValid = (realType ?? type).IsString();
            IsMulti = IsArray || IsList;
        }
        //  验证无效报错
        context.ReportErrorIf(IsValid == false, "[CacheKey]标记参数必须是string/Ilist<string>/string[]", parameter);
    }
    #endregion
}
