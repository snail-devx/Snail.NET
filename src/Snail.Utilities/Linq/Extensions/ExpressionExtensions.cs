using System.Linq.Expressions;
using System.Reflection;


namespace Snail.Utilities.Linq.Extensions;
/// <summary>
/// <see cref="Expression"/>扩展法昂发
/// </summary>
public static class ExpressionExtensions
{
    #region 公共方法
    /// <summary>
    /// 指定表达式是否是 比较 表达式：= != &gt; &gt;= &lt; &lt;=
    /// </summary>
    /// <param name="express"></param>
    /// <returns></returns>
    public static bool IsCompareExpress(this Expression express)
    {
        return express.NodeType switch
        {
            ExpressionType.Equal => true,
            ExpressionType.NotEqual => true,
            ExpressionType.GreaterThan => true,
            ExpressionType.GreaterThanOrEqual => true,
            ExpressionType.LessThan => true,
            ExpressionType.LessThanOrEqual => true,
            _ => false
        };
    }
    /// <summary>
    /// 指定表达式是否是 AndAlso、OrElse 表达式：&amp;&amp;  ||
    /// </summary>
    /// <param name="express"></param>
    /// <returns></returns>
    public static bool IsAndOrExpress(this Expression express)
        => express.NodeType switch
        {
            ExpressionType.AndAlso => true,
            ExpressionType.OrElse => true,
            _ => false,
        };

    /// <summary>
    /// 获取<typeparamref name="DbModel"/>属性、字段等成员表达式的具体成员信息<br />
    ///     1、如表达式为OrderBy(item=>item.Name) 则返回的是 Name 属性信息
    /// </summary>
    /// <typeparam name="DbModel">数据实体</typeparam>
    /// <typeparam name="TField">字段数据类型</typeparam>
    /// <param name="expression">lambda表达式，示例 item=>item.Name</param>
    /// <exception cref="ApplicationException">表达式无效会报错，从而确保返回值始终为DbModel的成员信息</exception>
    /// <returns>DbModel的属性成员信息</returns>
    public static MemberInfo GetMember<DbModel, TField>(this Expression<Func<DbModel, TField>> expression) where DbModel : class
    {
        return expression.Body is MemberExpression member
            ? member.Member
            : throw new ApplicationException($"expression格式错误。只能为{typeof(DbModel)}的属性/字段,正确示例:item=>item.Name");
    }
    /// <summary>
    /// 获取常量表达式中存储值
    /// </summary>
    /// <typeparam name="T">常量值类型</typeparam>
    /// <param name="express"></param>
    /// <returns></returns>
    public static T? GetConstValue<T>(this Expression express)
        => express.NodeType switch
        {
            ExpressionType.Constant => (T?)(express as ConstantExpression)!.Value,
            _ => throw new NotSupportedException($"不支持非【Constant】类型表达式获取值。当前表达式类型：{express.NodeType}")
        };
    #endregion
}
