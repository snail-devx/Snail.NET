using Snail.Database.Components;
using System.Linq.Expressions;

namespace Snail.Database.Utils;

/// <summary>
/// 数据库筛选条件助手类，负责对<see cref="Expression"/>查询条件做一些解析工作
/// </summary>
public static class DbFilterHelper
{
    #region 公共方法
    /// <summary>
    /// 获取like查询值；分析出like 后面的值是什么
    /// </summary>
    /// <param name="methodExpress"></param>
    /// <param name="isIgnoreCase">out 参数；like查询时是否忽略大小写</param>
    /// <returns></returns>
    public static string? GetLikeQueryValue(MethodCallExpression methodExpress, out bool isIgnoreCase)
    {
        //  验证支持的方法名称；不支持的先报错；这里暂不验证，外面也会做这个处理得到具体like语句
        //switch (methodExpress.Method.Name)
        //{
        //    case "Contains":
        //    case "StartsWith":
        //    case "EndsWith": break;
        //    default: throw new NotSupportedException($"不支持的like方法{methodExpress.Method.Name}：{methodExpress}");
        //}
        //  分析是否忽略大小写
        ConstantExpression? compareType = methodExpress.Arguments.Count >= 2
            ? methodExpress.Arguments[1] as ConstantExpression
            : null;
        switch (compareType?.Value)
        {
            case null:
                isIgnoreCase = false;
                break;
            case StringComparison comparison:
                isIgnoreCase = comparison == StringComparison.OrdinalIgnoreCase;
                break;
            case bool bValue:
                isIgnoreCase = bValue == true;
                break;
            //  其他情况暂时不支持
            default: throw new NotSupportedException($"不支持的like模式{compareType.Value}：{methodExpress}");
        }
        //  分析值
        return (methodExpress.Arguments[0] as ConstantExpression)?.Value?.ToString();
    }

    /// <summary>
    /// 获取in查询值；分析出in后面的数组值
    ///     举例：new List{T}(){}.Contains(item=>item.name)；分析出具体的list值
    ///     支持List和Array的Contains方法
    /// </summary>
    /// <param name="methodExpress"></param>
    /// <param name="member"></param>
    /// <returns></returns>
    public static object GetInQueryValues(MethodCallExpression methodExpress, out MemberExpression member)
    {
        if (methodExpress.Method.Name != "Contains")
        {
            string msg = "仅支持分析Contains方法调用值";
            throw new NotSupportedException(msg);
        }
        //  1、分析in查询的字段成员信息
        Expression tmpNode = methodExpress.Object == null ? methodExpress.Arguments[1] : methodExpress.Arguments[0];
        DbFilterFormatter.TryAnalysisMemberExpress(tmpNode, out member!);
        if (member == null)
        {
            string msg = $"构建in/nin时不支持的成员表达式：{tmpNode}";
            throw new NotSupportedException(msg);
        }
        //  2、分析in查询值和相关泛型类型：针对特定类型做一下兼容处理
        dynamic? inValues = AnalysisInQueryValues(methodExpress, out Type genericType);
        //  3、整理in查询值，进行类型反射创建；此处值必须显示构建。因为postgres数据库是强类型。不显示指定，in查询时会报错。
        Type listType = typeof(List<>).MakeGenericType(genericType!);
        dynamic values = Activator.CreateInstance(listType)!;
        foreach (var item in inValues!)
        {
            //  根据item的类型，做一些特定逻辑处理
            switch (item)
            {
                //  日期时间：查询时，外部可能传入的是2023/7/4 13:41:24，未带时区，此时需要做一下转换
                case DateTime dt:
                    values.Add(dt.ToUniversalTime());
                    break;
                case Enum en:
                    values.Add(Convert.ToInt32(en));
                    break;
                //  其他情况先默认
                default:
                    values.Add(item);
                    break;
            }
        }
        return values;
    }

    /// <summary>
    /// 从LastSortKey值中分析出skip数据值
    ///     1、mongodb等数据库，不能很好的支持ToResult逻辑；为了兼容ToResult接口，内部还是先使用Skip值
    /// </summary>
    /// <param name="lastSortKey"></param>
    /// <returns></returns>
    public static int GetSkipValueFromLastSortKey(string? lastSortKey)
    {
        Dictionary<string, int>? data = lastSortKey?.Any() == true
            ? lastSortKey.AsBase64Decode().As<Dictionary<string, int>>()
            : null;
        return data?.GetValueOrDefault("Skip") ?? 0;
    }
    /// <summary>
    /// 基于查询结构生成LastSortKey值
    ///     1、mongodb等数据库，不能很好的支持ToResult逻辑；为了兼容ToResult接口，内部还是先使用Skip值
    /// </summary>
    /// <param name="preSkipValue">上一页的skip值；第一页传0</param>
    /// <param name="pageCount">当前页查询出来的数据量</param>
    /// <returns></returns>
    public static string GenerateLastSortKeyBySkipValue(int preSkipValue, int pageCount)
    {
        //  把临时值做一些缓存，返回，方便后续做一些计算校验
        return preSkipValue == 0 && pageCount == 0
            ? string.Empty
            : new Dictionary<string, int>
            {
                {"PreSkip",preSkipValue },
                {"Page",pageCount },
                {"Skip",preSkipValue + pageCount },
            }.AsJson().AsBase64Encode();
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 分析in查询的参数值
    /// </summary>
    /// <param name="methodExpress"></param>
    /// <param name="genericType"></param>
    /// <returns></returns>
    private static dynamic? AnalysisInQueryValues(MethodCallExpression methodExpress, out Type genericType)
    {
        genericType = null!;
        dynamic? inValues = null;
        //  1、固定Array的.contains表达式，.net10会优化为ReadOnlySpan<T>，此时在【DbFilterFormatter.BuildMethodCallByContains】格式化时，转常量比较麻烦，还得反射查类型
        if (methodExpress.Method.DeclaringType == typeof(MemoryExtensions))
        {
            //  op_Implicit(new [] {Convert(Add, Nullable`1), null, Convert(GreaterThanOrEqual, Nullable`1)})
            MethodCallExpression methodNode = (methodExpress.Arguments[0] as MethodCallExpression)!;
            inValues = DbFilterFormatter.BuildConstant("Contains获取in数值", methodNode.Arguments[0]).Value;
            genericType = methodNode.Type.GenericTypeArguments[0];
        }
        //  2、List和Array的contains表达式
        else
        {
            ConstantExpression whoCall = (methodExpress.Object ?? methodExpress.Arguments[0]) as ConstantExpression
                ?? throw new NotSupportedException($"无法分析Contains调用方节点：{methodExpress}");
            _ = whoCall.Type.IsArray(out genericType!) || whoCall.Type.IsList(out genericType!);
            if (genericType == null)
            {
                string msg = $"IN筛选条件时，仅支持List和Array，当前为：{whoCall.Type.Name}。{methodExpress}";
                throw new NotSupportedException(msg);
            }
            inValues = whoCall.Value;
        }
        //  3、分析出的泛型数据类型做校验处理
        //      若为枚举类型时，当做int处理
        if (genericType!.IsEnum)
        {
            genericType = typeof(int);
        }
        else if (genericType.IsEnumNullable(out _))
        {
            genericType = typeof(int?);
        }

        return inValues;
    }
    #endregion
}
