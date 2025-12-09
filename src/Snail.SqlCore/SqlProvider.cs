using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.SqlCore.Components;
using Snail.SqlCore.Enumerations;
using Snail.SqlCore.Interfaces;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;

namespace Snail.SqlCore;

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>的【关系型数据库】抽象实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public abstract class SqlProvider<DbModel> : DbProvider, IDbModelProvider<DbModel>, ISqlDbRunner where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 表对象
    /// </summary>
    protected static readonly DbModelTable Table;

    /// <summary>
    /// 数据库中的表名称。做了关键字转义处理之后的
    /// </summary>
    protected readonly string DbTableName;
    /// <summary>
    /// 插入数据的sql语句；配合dapper插入数据，提前做好准备
    /// </summary>
    protected readonly string InsertSql;
    /// <summary>
    /// 获取数据的sql语句。示例：‘select id,name from tablename’ <br />
    ///     1、提前把【select 字段名 from 表名 】利索 <br />
    ///     2、避免 select * 和字段名、属性名对不上的情况 <br />
    /// </summary>
    protected readonly string SelectSql;

    /// <summary>
    /// 数据库工厂，用于构建不同类型数据库连接
    /// </summary>
    protected abstract DbProviderFactory DbFactory { get; }
    /// <summary>
    /// 关键字左侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected abstract string KeywordLeftToken { get; }
    /// <summary>
    /// 关键字右侧标记：用于解决sql关键字做表名、字段名时的问题
    /// </summary>
    protected abstract string KeywordRightToken { get; }
    /// <summary>
    /// 参数标记；防止sql注入时，参数化使用
    /// </summary>
    protected abstract string ParameterToken { get; }
    /// <summary>
    /// 数据库字段名称映射。
    ///     1、Key为属性名称，Value为对应的数据库字段名
    ///     2、数据库字段名，已进行关键字处理
    /// </summary>
    protected readonly IReadOnlyDictionary<string, string> DbFieldNameMap;
    /// <summary>
    /// 筛选条件过滤器
    /// </summary>
    protected readonly SqlFilterBuilder<DbModel> FilterBuilder;
    #endregion

    #region 构造方法
    /// <summary>
    /// 静态构造方法
    /// </summary>
    static SqlProvider()
    {
        Table = DbModelHelper.GetTable<DbModel>();
        //  暂时仅支持简单数据类型作为sql数据字段，后期再放开json等支持
        DbModelField? field = Table.Fields.FirstOrDefault(field => field.Type.IsBaseType() == false);
        if (field != null)
        {
            string msg = $"暂不支持此数据字段类型：{field.Property.Name}。类型：{field.Type}";
            throw new ApplicationException(msg);
        }
    }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public SqlProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
        //  1、分析表名字段名
        DbTableName = $"{KeywordLeftToken}{Table.Name}{KeywordRightToken}";
        List<string> dbFields = new(),
                     paramNames = new(),
                     selectFields = new();
        Dictionary<string, string> fieldMap = new Dictionary<string, string>();
        foreach (var field in Table.Fields)
        {
            string fieldName = $"{KeywordLeftToken}{field.Name}{KeywordRightToken}";
            fieldMap[field.Property.Name] = fieldName;
            //  临时集合处理，方便后续组装固定sql语句
            dbFields.Add(fieldName);
            paramNames.Add($"{ParameterToken}{field.Property.Name}");
            //  组装select查询字段信息；若和属性名称不一致，则需要做一下as操作
            if (field.Name != field.Property.Name)
            {
                fieldName = $"{fieldName} AS {KeywordLeftToken}{field.Property.Name}{KeywordRightToken}";
            }
            selectFields.Add(fieldName);
        }
        //      缓存字段映射；数据插入sql语句；全字段select语句
        DbFieldNameMap = new ReadOnlyDictionary<string, string>(fieldMap);
        InsertSql = $"INSERT INTO {DbTableName} ({dbFields.AsString(", ")}) VALUES({paramNames.AsString(", ")})";
        SelectSql = $"SELECT {selectFields.AsString(", ")} FROM {DbTableName}";
        //  2、其他默认属性字段值处理
        FilterBuilder = ThrowIfNull(GetFilterBuilder());
    }
    #endregion

    #region IDbModelProvider
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns>插入成功返回true；否则返回false</returns>
    async Task<bool> IDbModelProvider<DbModel>.Insert(IList<DbModel> models)
    {
        ThrowIfNullOrEmpty(models, "models为null或者空集合");
        //  遍历做验证；批量插入走事物，要么都成功，要么都失败
        await RunDbActionAsync(con => con.ExecuteAsync(InsertSql, models), false, true);
        return true;
    }
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    async Task<bool> IDbModelProvider<DbModel>.Save(IList<DbModel> models)
    {
        ThrowIfNullOrEmpty(models);
        ThrowIfHasNull(models!);
        //  先删除，后新增
        object[] ids = models
            .Select(instance =>
            {
                object pkValue = Table.PKField.Property.GetValue(instance)!;
                return DbModelHelper.BuildFieldValue(pkValue, Table.PKField)!;
            })
            .ToArray();
        string deleteSql = BuildDeleteSql(BuildIdFilter(ids, out var param));
        await RunDbActionAsync(async conn =>
        {
            await conn.ExecuteAsync(deleteSql, param);
            int count = await conn.ExecuteAsync(InsertSql, models);
            return count;
        }, false, true);
        return true;
    }
    /// <summary>
    /// 基于主键id值加载数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要加载的数据主键id值集合</param>
    /// <returns>数据实体集合</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsQueryable(string)"/>方法</remarks>
    async Task<IList<DbModel>> IDbModelProvider<DbModel>.Load<IdType>(IList<IdType> ids)
    {
        string filterSql = BuildIdFilter(ids, out IDictionary<string, object> param);
        string sql = BuildQuerySql(SelectUsageType.Data, filterSql);
        IEnumerable<DbModel> models = await RunDbActionAsync(con => con.QueryAsync<DbModel>(sql, param));
        return models?.ToList() ?? [];
    }
    /// <summary>
    /// 基于主键id值更新数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <param name="ids">要更新的数据主键id值集合</param>
    /// <returns>更新的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsUpdatable(string)"/>方法</remarks>
    async Task<long> IDbModelProvider<DbModel>.Update<IdType>(IDictionary<string, object?> updates, IList<IdType> ids)
    {
        // 关系型数据库中，主键标记和数据库无强制关系，这种情况也可能更新多条出来；不等于0就算成功
        string filterSql = BuildIdFilter(ids, out IDictionary<string, object> filterParam);
        string sql = BuildUpdateSql(updates, filterSql, filterParam, out IDictionary<string, object> param);
        long count = await RunDbActionAsync(con => con.ExecuteAsync(sql, param), false);
        return count;
    }
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsDeletable(string)"/>方法</remarks>
    async Task<long> IDbModelProvider<DbModel>.Delete<IdType>(params IList<IdType> ids)
    {
        string filterSql = BuildIdFilter(ids, out IDictionary<string, object> param);
        string sql = BuildDeleteSql(filterSql);
        long count = await RunDbActionAsync(con => con.ExecuteAsync(sql, param));
        return count;
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbModelProvider<DbModel>.AsQueryable(string? routing)
        => new SqlQueryable<DbModel>(Table.PKField.Property, this, FilterBuilder, routing);
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbModelProvider<DbModel>.AsUpdatable(string? routing)
        => new SqlUpdatable<DbModel>(this, FilterBuilder, routing);
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbModelProvider<DbModel>.AsDeletable(string? routing)
        => new SqlDeletable<DbModel>(this, FilterBuilder, routing);
    #endregion

    #region ISqlDbRunner
    /// <summary>
    /// 基于属性获取对应的数据库字段名称
    /// </summary>
    /// <param name="propertyName">属性名称</param>
    /// <param name="title">用途标题，报错使用</param>
    /// <returns></returns>
    public string GetDbFieldName(string propertyName, string title)
    {
        if (DbFieldNameMap.TryGetValue(propertyName, out string? dbFieldName) == false)
        {
            string msg = $"{title}：无法查找成员{propertyName}对应的数据库字段名称。DbModel：{typeof(DbModel)}";
        }
        return dbFieldName!;
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
    public abstract string BuildQuerySql(SelectUsageType usageType, string filterSql, IList<string>? selectFields = null,
        IList<KeyValuePair<string, bool>>? sorts = null, int? skip = null, int? take = null);
    /// <summary>
    /// 构建Update更新操作sql
    /// </summary>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <param name="filterParam">数据过滤条件参数字典；key为DbModel的参数名称，value为参数值</param>
    /// <param name="param">更新语句的参数化字典，自动合并过滤条件参数；key为参数名称，value为参数值。防止sql注入</param>
    /// <returns>完整可执行的sql语句</returns>
    public string BuildUpdateSql(IDictionary<string, object?> updates, string filterSql, IDictionary<string, object> filterParam, out IDictionary<string, object> param)
    {
        //  组装filter过滤条件：禁止无条件删除和无更新设置操作
        filterSql = Default(filterSql, defaultStr: null)!;
        ThrowIfNull(filterSql, $"数据过滤条件sql无效，禁止无条件构建更新语句：{filterSql}");
        //  组装更新的set语句：禁止无更新操作时调用
        ThrowIfNullOrEmpty(updates, "updates为null或者空字典；无需更新");
        string? setSql = null;
        param = new Dictionary<string, object>();
        {
            List<string> setFields = new();
            //  遍历更新做处理
            foreach (var kv in updates)
            {
                string fieldName = GetDbFieldName(kv.Key, title: nameof(BuildUpdateSql));
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
        return $"UPDATE {DbTableName} SET {setSql} WHERE {filterSql}";
    }
    /// <summary>
    /// 构建Delete删除操作sql
    /// </summary>
    /// <param name="filterSql">数据过滤条件sql；必填；不带Where条件；注意sql注入</param>
    /// <returns>完整可执行的sql语句</returns>
    public string BuildDeleteSql(string filterSql)
    {
        //  禁止无条件删除
        filterSql = Default(filterSql, defaultStr: null)!;
        ThrowIfNull(filterSql, $"数据过滤条件sql无效，禁止无条件构建删除语句：{filterSql}");
        return $"DELETE FROM {DbTableName} WHERE {filterSql}";
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
        if (isReadAction == true) needTransaction = false;
        //  构建连接，执行dbAction
        using DbConnection connection = CreateConnection(isReadAction);
        bool openInThis = false;
        IDbTransaction? transaction = null;
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
    public async Task<T> RunDbActionAsync<T>(Func<DbConnection, Task<T>> dbAction, bool isReadAction = true, bool needTransaction = false)
    {
        ThrowIfNull(dbAction);
        //  读操作不需要事务，做强制false
        if (isReadAction == true) needTransaction = false;
        //  构建连接，执行dbAction，注意是异步的
        //  ！！！后期考虑把里面部分异步换成同步，太多异步也不好，只在执行dbAction时做成异步的
        using DbConnection connection = CreateConnection(isReadAction);
        bool openInThis = false;
        IDbTransaction? transaction = null;
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
            transaction?.Commit();
            return retValue;
        }
        catch
        {
            transaction?.Rollback();
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
    #endregion

    #region 继承方法或重写方法
    /// <summary>
    /// 获取sql过滤条件构建器
    /// </summary>
    /// <returns></returns>
    protected virtual SqlFilterBuilder<DbModel> GetFilterBuilder()
    {
        return new SqlFilterBuilder<DbModel>(
            formatter: null,
            dbFieldNameFunc: pName => GetDbFieldName(pName, title: "dbFieldNameFunc"),
            parameterToken: ParameterToken
        );
    }

    /// <summary>
    /// 合并多个sql语句
    /// </summary>
    /// <param name="sqls">sql语句集合</param>
    /// <returns></returns>
    protected virtual string CombineSql(params string[] sqls) => sqls.AsString(";\r\n");

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <param name="isReadAction">是读操作，还是写操作；默认读操作</param>
    /// <returns></returns>
    protected DbConnection CreateConnection(bool isReadAction = true)
    {
        DbServerDescriptor dbServer = DbManager.GetServer(DbServer, isReadAction, null2Error: true)!;
        DbConnection connection = DbFactory.CreateConnection()
            ?? throw new ApplicationException($"调用Factory构建DbConnection为null：{DbFactory.GetType()}");
        //  设置数据库连接，并返回
        connection.ConnectionString = dbServer.Connection;
        return connection;
    }

    /// <summary>
    /// 构建select查询的字段sql
    ///     对字段进行as重命名，确保和DbModel的属性名称对应上
    /// </summary>
    /// <param name="selects">为null则直接返回所有字段</param>
    /// <returns>带有Select查询的语句；select 字段信息 from 表名称</returns>
    protected string BuildSelectSql(IList<string>? selects)
    {
        //  分析字段信息，组建as结构
        if (selects?.Count > 0)
        {
            IEnumerable<string> selectFields = selects
                .Select(pName => GetDbFieldName(pName, title: nameof(BuildSelectSql)));
            return $"SELECT {selectFields.AsString(",")} \r\nFROM {DbTableName}";

        }
        //  为空返回默认：所有字段
        return SelectSql;
    }

    /// <summary>
    /// 构建排序的sql语句
    /// </summary>
    /// <param name="sorts"></param>
    /// <returns>返回值不带 order by的排序信息。示例：Id ASC,Name DESC</returns>
    protected string? BuildSortSql(IList<KeyValuePair<string, bool>>? sorts)
    {
        return sorts?.Any() != true
            ? null
            : sorts.Select(kv =>
            {
                string dbFieldName = GetDbFieldName(kv.Key, title: nameof(BuildSortSql));
                return kv.Value == true ? $"{dbFieldName} ASC" : $"{dbFieldName} DESC";
            })
            .AsString(" , ");
    }
    /// <summary>
    /// 构建Id的过滤条件，支持单个和批量id值
    /// </summary>
    /// <param name="param">where条件参数化对象；key为参数名称，value为具体参数值</param>
    /// <param name="ids">主键id集合；支持一个或者多个id值</param>
    /// <returns>不带Where关键字的条件过滤语句，示例：id= @id 或者 id in $ids;</returns>
    protected virtual string BuildIdFilter<IdType>(IList<IdType> ids, out IDictionary<string, object> param)
    {
        //  需要对id值做类型处理，避免出现传值格式不对
        ThrowIfNullOrEmpty(ids, "ids为null或者空集合");
        ThrowIfHasNull(ids!, "ids中存在为null的数据");
        object[] newIds = ids.Select(item => DbModelHelper.BuildFieldValue(item!, Table.PKField)).ToArray()!;
        //  组装sql：针对一个数据和多个数据做=、in查询区分。一个时，参数名有主键字段属性名，兼容Save的用法
        if (newIds.Length == 1)
        {
            param = new Dictionary<string, object>().Set(Table.PKField.Property.Name, newIds.First());
            return $"{DbFieldNameMap[Table.PKField.Property.Name]} = {ParameterToken}{Table.PKField.Property.Name}";
        }
        else
        {
            param = new Dictionary<string, object>().Set("Ids", newIds);
            return $"{DbFieldNameMap[Table.PKField.Property.Name]} IN {ParameterToken}Ids";
        }
    }
    #endregion
}
