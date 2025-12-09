using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.SqlCore.Components;
using System.Linq.Expressions;

namespace Snail.PostgreSql.Components;

/// <summary>
/// Postgres数据库过滤条件构建器
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public sealed class PostgresFilterBuilder<DbModel> : SqlFilterBuilder<DbModel> where DbModel : class
{
    #region 属性变量
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="formatter">过滤表达式格式化器；为null使用默认的</param>
    /// <param name="dbFieldNameFunc">数据库字段名称处理委托；key为属性名，返回字段名</param>
    /// <param name="parameterToken">参数前缀</param>
    public PostgresFilterBuilder(DbFilterFormatter? formatter, Func<string, string> dbFieldNameFunc, string parameterToken)
        : base(formatter, dbFieldNameFunc, parameterToken)
    {
    }
    #endregion

    #region 重写父类
    /// <summary>
    /// 构建成员的In过滤条件
    ///     new String[]{}.Contains(item.Name);
    /// </summary>
    /// <param name="methodExpress"></param>
    /// <param name="isTrue"></param>
    /// <param name="param"></param>
    /// <returns></returns>
    protected override string BuildMemberInFilter(MethodCallExpression methodExpress, bool isTrue, IDictionary<string, object> param)
    {
        /*new List<String>().Contains(item.Name)   new String[].Contains(item.Name);*/

        //  暂时未测试，后期考虑

        //  分析出in查询的字段名称和in查询值
        object valuesT = DbFilterHelper.GetInQueryValues(methodExpress, out MemberExpression member);
        string field = GetDbFieldName(member.Member.Name);
        return isTrue
                    ? $"({field} IS NULL OR {field} = ANY({BuildSqlParameter(valuesT, param)}))"
                    : $"({field} IS NOT NULL AND {field} != ANY({BuildSqlParameter(valuesT, param)}))";

        //  ------------------------------------下面是父类的方法实现-------------------------------
        //  //  分析出in查询的字段名称和in查询值
        //  object valuesT = DbFilterHelper.GetInQueryValues(methodExpress, out MemberExpression member);
        //  List<object>? values = valuesT != null
        //      ? JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(valuesT))
        //      : null;
        //  string field = GetDbFieldName(member.Member.Name);
        //  //  分发构建查询：根据null和values分发处理；查询条件可能会有一些冗余，但代码可读性高
        //  /*      利用 |、switch分发，减少if、else if、else if、else的使用
        //   *      0|0 =0(无null，无有效值)  1|0 =1(仅有null值)；0|10 =10(仅有效值) 1|10=11(有null，有in值)*/
        //  int tmpIndex = (values?.RemoveAll(value => value == null) > 0 ? 1 : 0)
        //      | (values?.Count > 0 ? 10 : 0);
        //  switch (tmpIndex)
        //  {
        //      //  in []，恒false条件；not in []，恒true条件
        //      case 0:
        //          return isTrue
        //              ? "1 <> 1"
        //              : "1 = 1";
        //      //  in [null]，=null；not in [null]，!=null
        //      case 1:
        //          return isTrue
        //              ? $"{field} IS NULL"
        //              : $"{field} IS NOT NULL";
        //      //      in [...有效值]，in有效值；not in [...有效值]，=null、或者not in 有效值
        //      case 10:
        //          return isTrue
        //              ? $"{field} IN {BuildSqlParameter(values!, param)}"
        //              : $"({field} IS NULL OR {field} NOT IN {BuildSqlParameter(values!, param)})";
        //      //  in [null, ...有效值]，null、或者in有效值；not in [null, ...有效值]，!=null、且not in有效值
        //      case 11:
        //          return isTrue
        //              ? $"({field} IS NULL OR {field} IN {BuildSqlParameter(values!, param)})"
        //              : $"({field} IS NOT NULL AND {field} NOT IN {BuildSqlParameter(values!, param)})";
        //      //  默认情况：不会进入，预防一下，做强制报错处理
        //      default: throw new ApplicationException($"tmpIndex[{tmpIndex}]值异常；仅支持：0、1、10、11");
        //  };
    }
    #endregion
}
