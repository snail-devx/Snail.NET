using System.Data.Common;

namespace Snail.SqlCore.Interfaces;

/// <summary>
/// 关系型数据库提供程序
/// <para>1、约束关系型数据库的一些基础信息，如数据库类型，关键字等</para>
/// </summary>
public interface ISqlProvider : IDbProvider
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public Abstractions.Database.Enumerations.DbType DbType { get; }
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    public DbProviderFactory DbFactory { get; }
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public string KeywordLeftToken { get; }
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public string KeywordRightToken { get; }
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    public string ParameterToken { get; }
}