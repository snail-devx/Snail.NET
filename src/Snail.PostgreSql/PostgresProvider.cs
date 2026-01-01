using Npgsql;
using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.PostgreSql.Components;
using Snail.SqlCore.Components;
using Snail.Utilities.Collections.Extensions;
using System.Data.Common;

namespace Snail.PostgreSql;

/// <summary>
/// <see cref="IDbProvider"/>的PostgreSQL实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【Postgres】作为依赖注入key值 </para>
/// </summary>
[Component(From = typeof(IDbProvider), Key = nameof(DbType.Postgres), Lifetime = LifetimeType.Transient)]
public class PostgresProvider : SqlProvider, IDbProvider
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

    #region 属性重写
    /// <summary>
    /// 数据库类型
    /// </summary>
    public override DbType DbType => DbType.Postgres;
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    public override DbProviderFactory DbFactory => NpgsqlFactory.Instance;
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public override string KeywordLeftToken => "\"";
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public override string KeywordRightToken => "\"";
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    public override string ParameterToken => "@";
    #endregion

    #region 数据表信息处理
    #endregion

    #region SQL语句构建、处理
    /// <summary>
    /// 构建In查询条件
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="field">要进行in查询的字段</param>
    /// <param name="values">in查询值，这里涉及到类型不确定，强制为object，但实际值为<paramref name="field"/>中类型值</param>
    /// <param name="param">where条件参数化对象；key为参数名称，value为具体参数值</param>
    /// <returns>不带Where关键字的条件过滤语句，示例：id= @id 或者 id in $ids;</returns>
    public override string BuildInFilter<DbModel>(DbModelField field, object values, out IDictionary<string, object> param) where DbModel : class
    {
        string pkDbFieldName = GetDbFieldName<DbModel>(field.Property.Name, nameof(BuildInFilter));
        param = new Dictionary<string, object>().Set("Ids", values);
        return $"{pkDbFieldName} =ANY({ParameterToken}Ids)";
    }
    #endregion

    #region 其他处理
    /// <summary>
    /// 获取sql过滤条件构建器
    /// </summary>
    /// <returns></returns>
    public override SqlFilterBuilder<DbModel> GetFilterBuilder<DbModel>() where DbModel : class
    {
        // 使用自定义的过滤条件构建器
        return new PostgresFilterBuilder<DbModel>(
            formatter: null,
            dbFieldNameFunc: pName => GetDbFieldName<DbModel>(pName, title: "dbFieldNameFunc"),
            parameterToken: ParameterToken
        );
    }
    #endregion

    #endregion
}