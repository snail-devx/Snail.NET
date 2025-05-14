using System.Linq.Expressions;
using Snail.Database.Components;
using Snail.Utilities.Common.Extensions;

namespace Snail.Database.Utils
{
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
            //  分析in查询的字段成员信息
            Expression tmpNode = methodExpress.Object == null ? methodExpress.Arguments[1] : methodExpress.Arguments[0];
            DbFilterFormatter.TryAnalysisMemberExpress(tmpNode, out member!);
            if (member == null)
            {
                string msg = $"构建in/nin时不支持的成员表达式：{tmpNode}";
                throw new NotSupportedException(msg);
            }
            //  分析in查询值：分析出具体谁调用的Contains方法
            ConstantExpression whoCall = (methodExpress.Object ?? methodExpress.Arguments[0]) as ConstantExpression
                ?? throw new NotSupportedException($"无法分析Contains调用方节点：{methodExpress}");
            Type? genericType = null,
                  lgenericType = null,
                  listType = null;
            bool bValue = whoCall.Type.IsArray(out genericType) == true || whoCall.Type.IsList(out lgenericType) == true;
            if (bValue == false)
            {
                string msg = $"IN筛选条件时，仅支持List和Array，当前为：{whoCall.Type.Name}。{methodExpress}";
                throw new NotSupportedException(msg);
            }
            //      遍历整理查询值：注意对DateTime的处理，强制转成utc时间
            dynamic? value = whoCall.Value;
            genericType = genericType ?? lgenericType;
            //枚举类型时，当做int处理
            if (genericType!.IsEnum)
            {
                genericType = typeof(int);
            }
            else if (genericType.IsEnumNullable(out Type? enumN))
            {
                genericType = typeof(int?);
            }
            // 通过反射创建泛型列表类型
            listType = typeof(List<>).MakeGenericType(genericType);
            // 创建泛型列表对象:此处值必须显示构建。因为postgres数据库是强类型。不显示指定，in查询时会报错。
            dynamic values = Activator.CreateInstance(listType)!;
            foreach (var item in value!)
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
                ? lastSortKey.AsBase64Decode().As<Dictionary<String, Int32>>()
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
    }
}
