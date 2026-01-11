using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Database.Components;
using Snail.SqlCore.Components;
using Snail.SqlCore.Enumerations;
using Snail.SqlCore.Interfaces;
using Snail.Utilities.Collections.Extensions;
using System.Data;
using System.Data.Common;
using System.Text;
using static Snail.Database.Components.DbModelProxy;


namespace Snail.SqlCore;

/// <summary>
/// <see cref="IDbProvider"/>的【关系型数据库】抽象实现
/// </summary>
public abstract class SqlProvider : DbProvider, IDbProvider, ISqlProvider
{
    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">数据库服务器配置选项</param>
    public SqlProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
    #endregion

    #region ISqlProvider：abstract，交给具体子类重写
    /// <summary>
    /// 数据库类型
    /// </summary>
    public abstract Abstractions.Database.Enumerations.DbType DbType { get; }
    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    public abstract DbProviderFactory DbFactory { get; }
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public abstract string KeywordLeftToken { get; }
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    public abstract string KeywordRightToken { get; }
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    public abstract string ParameterToken { get; }
    #endregion

    #region IDbProvider
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns></returns>
    async Task<bool> IDbProvider.Insert<DbModel>(params IList<DbModel> models) where DbModel : class
    {
        ThrowIfNullOrEmpty(models, "models为null或者空集合");
        string insertSql = BuildInsertSql<DbModel>();
        //  遍历做验证；批量插入走事物，要么都成功，要么都失败
        await RunDbActionAsync(con => con.ExecuteAsync(insertSql, models), isReadAction: false, needTransaction: true);
        return true;
    }
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    async Task<bool> IDbProvider.Save<DbModel>(params IList<DbModel> models) where DbModel : class
    {
        ThrowIfNullOrEmpty(models);
        ThrowIfHasNull(models!);
        //  先删除，后新增
        DbModelField pkField = GetProxy<DbModel>().PKField;
        object ids = ExtractDbFieldValues(pkField, models);
        string deleteSql = deleteSql = BuildDeleteSql<DbModel>(BuildInFilter<DbModel>(pkField, ids, out var param));
        string insertSql = BuildInsertSql<DbModel>();
        await RunDbActionAsync(async conn =>
        {
            await conn.ExecuteAsync(deleteSql, param);
            //  这里需要强制await，避免pgsql等报错：Npgsql.NpgsqlOperationInProgressException:“A command is already in progress:
            return await conn.ExecuteAsync(insertSql, models);
        }, isReadAction: false, needTransaction: true);
        return true;
    }
    /// <summary>
    /// 基于主键id值加载数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要加载的数据主键id值集合</param>
    /// <returns>数据实体集合</returns>
    /// <remarks>
    /// <para> 1、不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsQueryable(string)"/>方法</para>
    /// </remarks>
    async Task<IList<DbModel>> IDbProvider.Load<DbModel, IdType>(IList<IdType> ids)
    {
        ThrowIfNullOrEmpty(ids);
        string filterSql = BuildInFilter<DbModel>(GetProxy<DbModel>().PKField, ids, out IDictionary<string, object> param);
        string sql = BuildQuerySql<DbModel>(SelectUsageType.Data, filterSql);
        IEnumerable<DbModel> models = await RunDbActionAsync(con => con.QueryAsync<DbModel>(sql, param), isReadAction: true, needTransaction: false);
        return models?.ToList() ?? [];
    }
    /// <summary>
    /// 基于主键id值更新数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要更新的数据主键id值集合</param>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <returns>更新的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
    async Task<long> IDbProvider.Update<DbModel, IdType>(IList<IdType> ids, IDictionary<string, object?> updates)
    {
        // 关系型数据库中，主键标记和数据库无强制关系，这种情况也可能更新多条出来；不等于0就算成功
        string filterSql = BuildInFilter<DbModel>(GetProxy<DbModel>().PKField, ids, out IDictionary<string, object> filterParam);
        string sql = BuildUpdateSql<DbModel>(updates, filterSql, filterParam, out IDictionary<string, object> param);
        long count = await RunDbActionAsync(con => con.ExecuteAsync(sql, param), isReadAction: false, needTransaction: true);
        return count;
    }
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsDeletable(string)"/>方法</remarks>
    async Task<long> IDbProvider.Delete<DbModel, IdType>(params IList<IdType> ids)
    {
        string filterSql = BuildInFilter<DbModel>(GetProxy<DbModel>().PKField, ids, out IDictionary<string, object> param);
        string sql = BuildDeleteSql<DbModel>(filterSql);
        long count = await RunDbActionAsync(con => con.ExecuteAsync(sql, param), isReadAction: false, needTransaction: true);
        return count;
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbProvider.AsQueryable<DbModel>(string? routing) where DbModel : class
        => new SqlQueryable<DbModel>(GetProxy<DbModel>().PKField.Property, this, GetFilterBuilder<DbModel>(), routing);
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbProvider.AsUpdatable<DbModel>(string? routing) where DbModel : class
        => new SqlUpdatable<DbModel>(this, GetFilterBuilder<DbModel>(), routing);
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbProvider.AsDeletable<DbModel>(string? routing) where DbModel : class
        => new SqlDeletable<DbModel>(this, GetFilterBuilder<DbModel>(), routing);
    #endregion

    #region 公共方法

    #region 数据表信息处理
    /// <summary>
    /// 获取数据库表名称
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <returns></returns>
    public virtual string GetTableName<DbModel>() where DbModel : class
        => DbTableInfoProxy<DbModel>.GetProxy(this).DbTableName;
    /// <summary>
    /// 基于属性获取对应的数据库字段名称
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="propertyName">属性名称</param>
    /// <param name="title">用途标题，报错使用</param>
    /// <returns></returns>
    public virtual string GetDbFieldName<DbModel>(string propertyName, string title) where DbModel : class
        => DbTableInfoProxy<DbModel>.GetProxy(this).GetDbFieldName(propertyName, title);
    #endregion

    #region SQL语句构建、处理
    /// <summary>
    /// 构建Select查询操作语句
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="usageType">select的用户，字段数据选择、any数据判断，数据量、、、</param>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="selectFields">需要返回的数据字段集合，值为DbModel属性名；为null、空则返回所有字段</param>
    /// <param name="sorts">排序配置，key为DbModel的属性名，Value为true升序/false降序</param>
    /// <param name="skip">分页跳过多少页</param>
    /// <param name="take">分页取多少页数据</param>
    /// <returns>完整可执行的sql查询语句</returns>
    public virtual string BuildQuerySql<DbModel>(SelectUsageType usageType, string filterSql, IList<string>? selectFields = null, IList<KeyValuePair<string, bool>>? sorts = null, int? skip = null, int? take = null)
        where DbModel : class
    {
        //  基于usagetype做一些分发
        switch (usageType)
        {
            //  data、any时，全量构建数据：做个兜底，any时，强制select 主键id
            case SelectUsageType.Data:
            case SelectUsageType.Any:
                selectFields = usageType == SelectUsageType.Any ? [GetProxy<DbModel>().PKField.Property.Name] : selectFields;
                string selectSql = BuildSelectSql<DbModel>(selectFields);
                string otherSql = BuildWhereSortLimitSql<DbModel>(filterSql, sorts, skip, take);
                return $"{selectSql} \r\n{otherSql}";
            //  select count(1)做逻辑；仅只用filtersql构建
            case SelectUsageType.Count:
                return $"SELECT COUNT(1) \r\nFROM {GetTableName<DbModel>()} \r\nWHERE {filterSql}";
            default: throw new NotSupportedException($"BuildQuerySql:不支持的{nameof(usageType)}值[{usageType.ToString()}]");
        }
    }
    /// <summary>
    /// 构建In查询条件
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="field">要进行in查询的字段</param>
    /// <param name="values">in查询值，这里涉及到类型不确定，强制为object，但实际值为<paramref name="field"/>中类型值</param>
    /// <param name="param">where条件参数化对象；key为参数名称，value为具体参数值</param>
    /// <returns>不带Where关键字的条件过滤语句，示例：id= @id 或者 id in $ids;</returns>
    public virtual string BuildInFilter<DbModel>(DbModelField field, object values, out IDictionary<string, object> param) where DbModel : class
    {
        string pkDbFieldName = GetDbFieldName<DbModel>(field.Property.Name, nameof(BuildInFilter));
        param = new Dictionary<string, object>().Set("Ids", values);
        return $"{pkDbFieldName} IN {ParameterToken}Ids";
    }
    /// <summary>
    /// 构建select查询的字段sql
    /// <para>1、对字段进行as重命名，确保和DbModel的属性名称对应上</para>
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="selects">为null则直接返回所有字段</param>
    /// <returns>带有Select查询的语句；select 字段信息 from 表名称</returns>
    protected virtual string BuildSelectSql<DbModel>(IList<string>? selects) where DbModel : class
    {
        string tableName = GetTableName<DbModel>();
        //  分析字段信息，组建as结构
        if (IsNullOrEmpty(selects) == false)
        {
            IEnumerable<string> selectFields = selects!.Select(pName => GetDbFieldName<DbModel>(pName, title: nameof(BuildSelectSql)));
            return $"SELECT {selectFields.AsString(",")} \r\nFROM {tableName}";
        }
        //  为空返回默认：所有字段
        return DbTableInfoProxy<DbModel>.GetProxy(this).SelectSql;
    }
    /// <summary>
    /// 构建排序的sql语句
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="sorts"></param>
    /// <returns>返回值不带 order by的排序信息。示例：Id ASC,Name DESC</returns>
    protected virtual string? BuildSortSql<DbModel>(IList<KeyValuePair<string, bool>>? sorts) where DbModel : class
    {
        return sorts?.Any() != true
            ? null
            : sorts.Select(kv =>
            {
                string dbFieldName = GetDbFieldName<DbModel>(kv.Key, title: nameof(BuildSortSql));
                return kv.Value == true ? $"{dbFieldName} ASC" : $"{dbFieldName} DESC";
            })
            .AsString(" , ");
    }
    /// <summary>
    /// 构建Where+Sort+Limit相关sql语句
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="sorts">排序配置，key为DbModel的属性名，Value为true升序/false降序</param>
    /// <param name="skip">分页跳过多少页</param>
    /// <param name="take">分页取多少页数据</param>
    /// <returns>带有WHERE 的sql语句</returns>
    protected virtual string BuildWhereSortLimitSql<DbModel>(string filterSql, IList<KeyValuePair<string, bool>>? sorts, int? skip, int? take)
        where DbModel : class
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
            string sortSql = BuildSortSql<DbModel>(sorts)!;
            sb.AppendLine($"ORDER BY {sortSql}");
        }
        //  组装分页：未传分页条件，则保持现状
        if (skip != null || take != null)
        {
            //  不指定take值，按照道理来说，可指定-1；但mysql 5.7.17执行时不支持，先给个int的最大值。但会影响性能
            take ??= int.MaxValue;
            sb.AppendLine(skip == null ? $"LIMIT {take}" : $"LIMIT {skip},{take}");
        }
        //  构建返回
        return sb.ToString();
    }

    /// <summary>
    /// 构建插入数据的sql语句
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <returns></returns>
    public virtual string BuildInsertSql<DbModel>() where DbModel : class
        => DbTableInfoProxy<DbModel>.GetProxy(this).InsertSql;
    /// <summary>
    /// 构建Update更新操作sql
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="filterParam">数据过滤条件参数字典；key为DbModel的参数名称，value为参数值</param>
    /// <param name="param">更新语句的参数化字典，自动合并过滤条件参数；key为参数名称，value为参数值。防止sql注入</param>
    /// <returns>完整可执行的sql语句</returns>
    public virtual string BuildUpdateSql<DbModel>(IDictionary<string, object?> updates, string filterSql, IDictionary<string, object> filterParam, out IDictionary<string, object> param)
        where DbModel : class
    {
        //  组装filter过滤条件：禁止无条件删除和无更新设置操作
        filterSql = Default(filterSql, defaultStr: null)!;
        ThrowIfNull(filterSql, $"数据过滤条件sql无效，禁止无条件构建更新语句：{filterSql}");
        //  组装更新的set语句：禁止无更新操作时调用
        ThrowIfNullOrEmpty(updates, "updates为null或者空字典；无需更新");
        string? setSql = null;
        param = new Dictionary<string, object>();
        {
            List<string> setFields = [];
            //  遍历更新做处理
            foreach (var kv in updates)
            {
                string fieldName = GetDbFieldName<DbModel>(kv.Key, title: nameof(BuildUpdateSql));
                //  后续考虑在这里做必填等字段格式值验证；现在先保持现状
                //  对参数名做一下差异化，避免和where条件中的param冲突重复
                param!.Set($"U_{kv.Key}", kv.Value);
                setFields.Add($"{fieldName} = {ParameterToken}U_{kv.Key}");
            }
            setSql = setFields.AsString(" , ");
            //  合并filter参数
            param = param.Combine(filterParam!);
        }
        ThrowIfNull(setSql, $"组装出来的set语句为空，无法进行更新操作：{updates}");
        //  组装语句返回
        return $"UPDATE {GetTableName<DbModel>()} SET {setSql} WHERE {filterSql}";
    }
    /// <summary>
    /// 构建Delete删除操作sql
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <returns>完整可执行的sql语句</returns>
    public virtual string BuildDeleteSql<DbModel>(string filterSql) where DbModel : class
    {
        //  禁止无条件删除
        filterSql = Default(filterSql, defaultStr: null)!;
        ThrowIfNull(filterSql, $"数据过滤条件sql无效，禁止无条件构建删除语句：{filterSql}");
        return $"DELETE FROM {GetTableName<DbModel>()} WHERE {filterSql}";
    }

    /// <summary>
    /// 合并多个sql语句
    /// </summary>
    /// <param name="sqls">sql语句集合</param>
    /// <returns></returns>
    public virtual string CombineSql(params string[] sqls) => sqls.AsString(";\r\n");
    #endregion

    #region 其他处理
    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <param name="isReadAction">是读操作，还是写操作；默认读操作</param>
    /// <returns></returns>
    public virtual DbConnection CreateConnection(bool isReadAction = true)
    {
        DbServerDescriptor dbServer = DbManager.GetServer(DbServer, isReadAction, null2Error: true)!;
        DbConnection connection = DbFactory.CreateConnection()
            ?? throw new ApplicationException($"调用Factory构建DbConnection为null：{DbFactory.GetType()}");
        //  设置数据库连接，并返回
        connection.ConnectionString = dbServer.Connection;
        return connection;
    }
    /// <summary>
    /// 执行数据库操作
    /// </summary>
    /// <typeparam name="T">执行数据库操作返回值类型</typeparam>
    /// <param name="dbAction">要执行的数据库操作</param>
    /// <param name="isReadAction">操作数据库时，是读操作还是写操作；默认读操作</param>
    /// <param name="needTransaction">是否需要事务，默认不需要</param>
    /// <returns>数据库操作返回值</returns>
    public T RunDbAction<T>(Func<DbConnection, T> dbAction, bool isReadAction = true, bool needTransaction = false)
    {
        ThrowIfNull(dbAction);
        //  读操作，不需要事务，做强制false
        if (isReadAction == true)
        {
            needTransaction = false;
        }
        //  构建连接，执行dbAction
        using DbConnection connection = CreateConnection(isReadAction);
        bool openInThis = false;
        DbTransaction? transaction = null;
        //  执行操作：数据库连接关闭了，则重新打开；根据需要启动事务
        try
        {
            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
                openInThis = true;
            }
            if (needTransaction == true)
            {
                transaction = connection.BeginTransaction();
            }
            T retValue = dbAction(connection);
            transaction?.Commit();
            return retValue;
        }
        //  异常回滚
        catch
        {
            transaction?.Rollback();
            throw;
        }
        //  当前方法打开了数据库连接，记得关闭
        finally
        {
            if (openInThis == true)
            {
                connection.Close();
            }
        }
    }
    /// <summary>
    /// 执行异步数据库操作
    /// </summary>
    /// <typeparam name="T">执行数据库操作返回值类型</typeparam>
    /// <param name="dbAction">要执行的数据库操作</param>
    /// <param name="isReadAction">操作数据库时，是读操作还是写操作；默认读操作</param>
    /// <param name="needTransaction">是否需要事务，默认不需要</param>
    /// <returns>数据库操作返回值</returns>
    async public Task<T> RunDbActionAsync<T>(Func<DbConnection, Task<T>> dbAction, bool isReadAction = true, bool needTransaction = false)
    {
        ThrowIfNull(dbAction);
        //  读操作不需要事务，做强制false
        if (isReadAction == true)
        {
            needTransaction = false;
        }
        //  构建连接，执行dbAction，注意是异步的
        //  ！！！后期考虑把里面部分异步换成同步，太多异步也不好，只在执行dbAction时做成异步的
        using DbConnection connection = CreateConnection(isReadAction);
        bool openInThis = false;
        DbTransaction? transaction = null;
        try
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
                openInThis = true;
            }
            if (needTransaction == true)
            {
                transaction = await connection.BeginTransactionAsync();
            }
            T retValue = await dbAction(connection);
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
            return retValue;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            if (openInThis == true)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// 获取sql过滤条件构建器
    /// </summary>
    /// <returns></returns>
    public virtual SqlFilterBuilder<DbModel> GetFilterBuilder<DbModel>() where DbModel : class
    {
        string DbFieldNameFunc(string pName) => GetDbFieldName<DbModel>(pName, title: "dbFieldNameFunc");
        return new SqlFilterBuilder<DbModel>(formatter: null, dbFieldNameFunc: DbFieldNameFunc, parameterToken: ParameterToken);
    }
    #endregion

    #endregion
}