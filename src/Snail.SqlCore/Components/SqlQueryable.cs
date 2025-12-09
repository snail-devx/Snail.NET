using Snail.Abstractions.Database.DataModels;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.SqlCore.Enumerations;
using Snail.SqlCore.Interfaces;
using System.Reflection;

namespace Snail.SqlCore.Components;

/// <summary>
/// <see cref="IDbQueryable{DbModel}"/>接口的关系型数据库实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public class SqlQueryable<DbModel> : DbQueryable<DbModel>, IDbQueryable<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 主键属性
    /// </summary>
    protected readonly PropertyInfo PKProperty;
    /// <summary>
    /// 过滤条件构建器
    /// </summary>
    protected readonly SqlFilterBuilder<DbModel> FilterBuilder;
    /// <summary>
    /// sql数据库运行器
    /// </summary>
    protected readonly ISqlDbRunner Runner;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="pkProperty">主键属性</param>
    /// <param name="runner">运行器</param>
    /// <param name="builder">过滤条件构建器/></param>
    /// <param name="routing">路由信息</param>
    public SqlQueryable(PropertyInfo pkProperty, ISqlDbRunner runner, SqlFilterBuilder<DbModel> builder, string? routing)
        : base(routing)
    {
        PKProperty = ThrowIfNull(pkProperty);
        Runner = ThrowIfNull(runner);
        FilterBuilder = ThrowIfNull(builder);
    }
    #endregion

    #region IDbQueryable：部分交给DbQueryable做默认实现
    /// <summary>
    /// 符合Where条件的【所有数据】条数
    /// </summary>
    /// <remarks>仅使用Where条件做查询；Skip、Take、Order等失效</remarks>
    /// <returns>符合条件的数据条数</returns>
    public override async Task<long> Count()
    {
        string sql = BuildQuery(SelectUsageType.Count, out IDictionary<string, object> param, sorts: out _);
        long count = await Runner.RunDbActionAsync(conn => conn.ExecuteScalarAsync<Int64>(sql, param), true, false);
        return count;
    }
    /// <summary>
    /// 是否存在符合条件的数据
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>存在返回true；否则返回false</returns>
    public override async Task<bool> Any()
    {
        //  强制值需要1条数据；先使用DbModel做泛型转换，后续考虑做一些其他逻辑处理，如select 1 这类操作
        string sql = BuildQuery(SelectUsageType.Any, out IDictionary<string, object> param, sorts: out _, take: 1);
        IEnumerable<DbModel> ret = await Runner.RunDbActionAsync(con => con.QueryAsync<DbModel>(sql, param), true, false);
        return ret.Any();
    }

    /// <summary>
    /// 获取符合条件的第一条数据；
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>数据实体；无则返回默认值</returns>
    public override async Task<DbModel?> FirstOrDefault()
    {
        //  强制值需要1条数据；
        string sql = BuildQuery(SelectUsageType.Data, out IDictionary<string, object> param, sorts: out _, take: 1);
        DbModel? data = await Runner.RunDbActionAsync(con => con.QueryFirstOrDefaultAsync(sql, param), true, false);
        return data;
    }
    /// <summary>
    /// 获取符合筛选条件+分页的所有数据<br />
    ///     1、禁止无条件查询
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>数据库实体集合</returns>
    public override async Task<IList<DbModel>> ToList()
    {
        string sql = BuildQuery(SelectUsageType.Data, out IDictionary<string, object> param, sorts: out _);
        IEnumerable<DbModel> result = await Runner.RunDbActionAsync(con => con.QueryAsync<DbModel>(sql, param), true, false);
        return result?.ToList() ?? [];
    }
    /// <summary>
    /// 获取符合筛选条件+分页的查询结果<br />
    ///     1、支持LastSortKey逻辑
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns></returns>
    public async override Task<DbQueryResult<DbModel>> ToResult()
    {
        string sql = BuildQuery(SelectUsageType.Data, out IDictionary<string, object> param, out var sorts, needSortField: true);
        IEnumerable<DbModel> datas = await Runner.RunDbActionAsync(con => con.QueryAsync<DbModel>(sql, param), true, false);
        //  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
        //return new DbQueryResult<DbModel>(datas).BuildLastSortKey(sorts);
        DbQueryResult<DbModel> ret = new DbQueryResult<DbModel>(datas?.ToArray());
        ret.LastSortKey = DbFilterHelper.GenerateLastSortKeyBySkipValue(Skip ?? 0, ret.Page ?? 0);
        return ret;
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 构建Select查询操作
    /// </summary>
    /// <param name="usageType">select的用户，字段数据选择、any数据判断，数据量、、、</param>
    /// <param name="param">out参数：过滤条件中的参数化对象，key为参数名称、value为参数值</param>
    /// <param name="sorts">out参数：整理之后的排序字段信息；key为属性名，value为升序还是降序</param>
    /// <param name="take">需要取几条数据，为null则使用全局的<see cref="DbQueryable{DbModel}.Take"/>值；满足first、any等特殊逻辑</param>
    /// <param name="needSortField">是否需要返回排序字段值，在ToResult时，需传true；否则会导致lastsortkey出问题</param>
    /// <returns>完整可执行的sql查询语句</returns>
    protected string BuildQuery(SelectUsageType usageType, out IDictionary<string, object> param, out IList<KeyValuePair<string, bool>>? sorts, int? take = null, bool needSortField = false)
    {
        //  全局的Where条件构建
        string filterSql = FilterBuilder.BuildFilter(Filters, out param);
        //  按照usageType类型做一下逻辑分发
        switch (usageType)
        {
            //  Data ,选取数据：构建全量查询（包括skip、take等）；Selects需要根据需要包含sort字段
            //  Any,判断存在性：构建全量查询，但Select仅包含主键字段
            case SelectUsageType.Data:
            case SelectUsageType.Any:
                {
                    //  整理sorts值，并基于LastSortKey构建查询过滤条件
                    sorts = GetSorts(PKProperty.Name);
                    /*  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
                    String sortFilter = BuildLastSortKeyFilter(LastSortKey, sorts, param);
                    filterSql = sortFilter == null
                        ? filterSql
                        : $" ({Filters}) AND ({sortFilter}) ";
                    */
                    //  构建select字段信息：Any时强制仅select 主键字段；其他时候根据需要来
                    List<string> selectFields;
                    if (usageType == SelectUsageType.Any)
                    {
                        selectFields = [PKProperty.Name];
                    }
                    /*  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
                    else if (Selects.Count > 0 && needSortField == true)
                    {
                        selectFields = new List<String>();
                        selectFields.AddRange(Selects);
                        selectFields.AddRange(sorts.Select(sort => sort.Key));
                        selectFields = selectFields.Distinct().ToList();
                    //}
                    */
                    else
                    {
                        selectFields = Selects;
                    }
                    //  组装查询sql：skip和take做默认值处理，lastsortkey模式下，skip强制为0
                    take = take ?? Take;
                    /*  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
                    Int32? skip = LastSortKey?.Length > 0 ? null : Skip;
                     */
                    if (LastSortKey?.Length > 0)
                    {
                        Int32 skip = DbFilterHelper.GetSkipValueFromLastSortKey(LastSortKey);
                        (this as IDbQueryable<DbModel>).Skip(skip);
                    }
                    return Runner.BuildQuerySql(usageType, filterSql, selectFields, sorts, Skip, take);
                }
            //  获取数据总量：仅构建Where条件查询
            case SelectUsageType.Count:
                {
                    sorts = null;
                    return Runner.BuildQuerySql(SelectUsageType.Count, filterSql);
                }
            //  不支持，直接报错
            default:
                {
                    string msg = $"BuildQuery:不支持的{nameof(usageType)}值[{usageType.ToString()}]";
                    throw new NotSupportedException(msg);
                }
        }
    }
    #endregion
}
