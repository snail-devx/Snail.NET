using MySql.Data.MySqlClient;
using Snail.Abstractions;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using System.Data.Common;

namespace Snail.MySql;

/// <summary>
/// <see cref="IDbProvider"/>的MySql实现
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【MySql】作为依赖注入key值 </para>
/// </summary>
[Component(From = typeof(IDbProvider), Key = nameof(DbType.MySql), Lifetime = LifetimeType.Transient)]
public class MySqlProvider : SqlProvider, IDbProvider
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

    #region 属性重写
    /// <summary>
    /// 数据库类型
    /// </summary>
    public override DbType DbType => DbType.MySql;
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    public override DbProviderFactory DbFactory => MySqlClientFactory.Instance;
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public override string KeywordLeftToken => "`";
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public override string KeywordRightToken => "`";
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    public override string ParameterToken => "?";
    #endregion

    #region SQL语句构建、处理

    #endregion

    #region 表、字段名称

    #endregion

    #endregion
}