using MySql.Data.MySqlClient;
using Snail.Abstractions;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.SqlCore.Enumerations;
using System.Data.Common;
using System.Text;

namespace Snail.MySql;

/// <summary>
/// <see cref="IDbModelProvider{DbModel,IdType}"/>的MySql实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【MySql】作为依赖注入key值 </para>
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
/// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
[Component(From = typeof(IDbModelProvider<,>), Key = nameof(DbType.MySql), Lifetime = LifetimeType.Transient)]
public class MySqlProvider<DbModel, IdType> : SqlProvider<DbModel, IdType>, IDbModelProvider<DbModel, IdType>
    where DbModel : class where IdType : notnull
{
    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public MySqlProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
    #endregion

    #region 重写父类
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    protected override DbProviderFactory DbFactory => MySqlClientFactory.Instance;
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected override string KeywordLeftToken => "`";
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected override string KeywordRightToken => "`";
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    protected override string ParameterToken => "?";

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
            //  不指定take值，按照道理来说，可指定-1；但5.7.17执行时不支持，先给个int的最大值。但会影响性能
            take ??= int.MaxValue;
            sb.AppendLine(skip == null ? $"LIMIT {take}" : $"LIMIT {skip},{take}");
        }
        //  构建返回
        return sb.ToString();
    }
    #endregion
}

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>的MySql实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【MySql】作为依赖注入key值 </para>
/// <para>2、数据实体的IdType强制为string</para>
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
[Component(From = typeof(IDbModelProvider<>), Key = nameof(DbType.MySql), Lifetime = LifetimeType.Transient)]
public class MySqlProvider<DbModel> : MySqlProvider<DbModel, string>, IDbModelProvider<DbModel> where DbModel : class
{
    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public MySqlProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
    #endregion
}