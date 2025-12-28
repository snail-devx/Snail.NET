using Npgsql;
using Snail.Abstractions;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.Database.Utils;
using Snail.PostgreSql.Components;
using Snail.SqlCore.Components;
using Snail.SqlCore.Enumerations;
using Snail.Utilities.Collections.Extensions;
using System.Data.Common;
using System.Text;

namespace Snail.PostgreSql;

/// <summary>
/// <see cref="IDbModelProvider{DbModel,IdType}"/>的PostgreSQL实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【Postgres】作为依赖注入key值 </para>
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
/// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
[Component(From = typeof(IDbModelProvider<,>), Key = nameof(DbType.Postgres), Lifetime = LifetimeType.Transient)]
public class PostgresProvider<DbModel, IdType> : SqlProvider<DbModel, IdType>, IDbModelProvider<DbModel, IdType>
    where DbModel : class where IdType : notnull
{
    #region 属性变量
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public PostgresProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
    #endregion

    #region 重写父类
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    protected override DbProviderFactory DbFactory => NpgsqlFactory.Instance;
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected override string KeywordLeftToken => "\"";
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected override string KeywordRightToken => "\"";
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    protected override string ParameterToken => "@";

    /// <summary>
    /// 获取sql过滤条件构建器
    /// </summary>
    /// <returns></returns>
    protected override SqlFilterBuilder<DbModel> GetFilterBuilder()
    {
        // 使用自定义的过滤条件构建器
        return new PostgresFilterBuilder<DbModel>(
            formatter: null,
            dbFieldNameFunc: pName => GetDbFieldName(pName, title: "dbFieldNameFunc"),
            parameterToken: ParameterToken
        );
    }

    /// <summary>
    /// 构建Select查询操作语句
    /// </summary>
    /// <param name="usageType">select的用户，字段数据选择、any数据判断，数据量、、、</param>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="selectFields">需要返回的数据字段集合，值为DbModel属性名；为null、空则返回所有字段</param>
    /// <param name="sorts">排序配置，key为DbModel的属性名，Value为true升序/false降序</param>
    /// <param name="skip">分页跳过多少页</param>
    /// <param name="take">分页取多少页数据</param>
    /// <returns>完整可执行的sql查询语句</returns>
    public override string BuildQuerySql(SelectUsageType usageType, string filterSql, IList<string>? selectFields = null,
        IList<KeyValuePair<string, bool>>? sorts = null, int? skip = null, int? take = null)
    {
        //  基于usagetype做一些分发
        switch (usageType)
        {
            //  data、any时，全量构建数据：做个兜底，any时，强制select 主键id
            case SelectUsageType.Data:
            case SelectUsageType.Any:
                selectFields = usageType == SelectUsageType.Any ? [Table.PKField.Property.Name] : selectFields;
                string selectSql = BuildSelectSql(selectFields);
                string otherSql = BuildWhereSortLimitSql(filterSql, sorts, skip, take);
                return $"{selectSql} \r\n{otherSql}";
            //  select count(1)做逻辑；仅只用filtersql构建
            case SelectUsageType.Count:
                return $"SELECT COUNT(1) \r\nFROM {DbTableName} \r\nWHERE {filterSql}";
            default: throw new NotSupportedException($"BuildQuerySql:不支持的{nameof(usageType)}值[{usageType.ToString()}]");
        }
    }
    /// <summary>
    /// 构建Id的过滤条件，支持单个和批量id值
    /// </summary>
    /// <param name="param">where条件参数化对象；key为参数名称，value为具体参数值</param>
    /// <param name="ids">主键id集合；支持一个或者多个id值</param>
    /// <returns>不带Where关键字的条件过滤语句，示例：id= @id 或者 id in $ids;</returns>
    protected override string BuildIdFilter(IList<IdType> ids, out IDictionary<string, object> param)
    {
        //  需要对id值做类型处理，避免出现传值格式不对
        ThrowIfNullOrEmpty(ids, "ids为null或者空集合");
        ThrowIfHasNull(ids!, "ids中存在为null的数据");
        IdType[] newIds = ids.Select(item => (IdType)DbModelHelper.BuildFieldValue(item!, Table.PKField)!).ToArray()!;
        //  组装sql：针对一个数据和多个数据做=、in查询区分。一个时，参数名有主键字段属性名，兼容Save的用法
        if (newIds.Length == 1)
        {
            param = new Dictionary<string, object>().Set(Table.PKField.Property.Name, newIds.First()!);
            return $"{DbFieldNameMap[Table.PKField.Property.Name]} = {ParameterToken}{Table.PKField.Property.Name}";
        }
        else
        {
            param = new Dictionary<string, object>().Set("Ids", newIds);
            return $"{DbFieldNameMap[Table.PKField.Property.Name]} IN {ParameterToken}Ids";
        }
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建Where+Sort+Limit相关sql语句
    /// </summary>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="sorts">排序配置，key为DbModel的属性名，Value为true升序/false降序</param>
    /// <param name="skip">分页跳过多少页</param>
    /// <param name="take">分页取多少页数据</param>
    /// <returns>带有WHERE 的sql语句</returns>
    private string BuildWhereSortLimitSql(string filterSql, IList<KeyValuePair<string, bool>>? sorts, int? skip, int? take)
    {
        StringBuilder sb = new StringBuilder();
        //  组装where条件：禁止无条件操作
        {
            ThrowIfNullOrEmpty(filterSql, $"数据过滤条件sql无效，禁止无条件构建查询语句：{filterSql}");
            sb.AppendLine($"WHERE {filterSql}");
        }
        //  组装排序
        if (sorts?.Count > 0)
        {
            string sortSql = BuildSortSql(sorts)!;
            sb.AppendLine($"ORDER BY {sortSql}");
        }
        //  组装分页：未传分页条件，则保持现状
        if (skip != null || take != null)
        {
            take ??= int.MaxValue;
            sb.AppendLine(skip == null ? $"LIMIT {take}" : $"LIMIT {skip},{take}");
        }
        //  构建返回
        return sb.ToString();
    }
    #endregion
}

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>的PostgreSQL实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【Postgres】作为依赖注入key值 </para>
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
[Component(From = typeof(IDbModelProvider<>), Key = nameof(DbType.Postgres), Lifetime = LifetimeType.Transient)]
public sealed class PostgresProvider<DbModel> : PostgresProvider<DbModel, string>, IDbModelProvider<DbModel> where DbModel : class
{
    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public PostgresProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
    #endregion
}