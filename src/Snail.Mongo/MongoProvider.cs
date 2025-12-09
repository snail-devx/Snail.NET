using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.Mongo.Components;
using Snail.Mongo.Utils;

namespace Snail.Mongo;

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>的MongoDB实现 <br />
///     1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【MongoDB】作为依赖注入key值
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
[Component(From = typeof(IDbModelProvider<>), Key = nameof(DbType.MongoDB), Lifetime = LifetimeType.Transient)]
public class MongoProvider<DbModel> : DbProvider, IDbModelProvider<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 表对象
    /// </summary>
    protected static readonly DbModelTable Table;
    #endregion

    #region 构造方法
    /// <summary>
    /// 静态构造方法
    /// </summary>
    static MongoProvider()
    {
        MongoHelper.RegisterClassMap(typeof(DbModel));
        Table = DbModelHelper.GetTable(typeof(DbModel));
    }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public MongoProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
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
        IList<BsonDocument> documents = models.Select(MongoHelper.BuildDocument).ToList();
        await CreateBsonCollection(false).InsertManyAsync(documents);
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
        //  将实体直接反序列化成Document，少了几次DbModel<->BsonDocument的转换，对性能友好
        IMongoCollection<BsonDocument> collection = CreateBsonCollection(false);
        List<BsonDocument> inserts = new List<BsonDocument>();
        foreach (var model in models)
        {
            BsonDocument document = MongoHelper.BuildDocument(model);
            BsonValue pkValue = document.GetValue(MongoHelper.PK_FIELDNAME);
            BsonDocument tmpDocument = new(MongoHelper.PK_FIELDNAME, pkValue);
            //  执行查找替换，不成功则补偿插入一下
            tmpDocument = await collection.FindOneAndReplaceAsync<BsonDocument>(tmpDocument, document);
            if (tmpDocument == null)
            {
                inserts.Add(document);
            }
        }
        //      做补偿插入逻辑
        if (inserts.Count > 0)
        {
            await collection.InsertManyAsync(inserts);
        }
        //  不报错标记为成功
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
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = MongoHelper.BuildFieldFilter(Table.PKField, ids);
        IFindFluent<DbModel, DbModel> fluent = CreateCollection(isReadonly: true).Find(filter);
        IList<DbModel> list = await fluent.ToListAsync();
        return list ?? [];
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
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = MongoHelper.BuildFieldFilter<IdType>(Table.PKField, ids);
        UpdateDefinition<DbModel> update = MongoHelper.BuildUpdate<DbModel>(updates);
        UpdateResult result = await CreateCollection(false).UpdateManyAsync(filter, update);
        return result.ModifiedCount;
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
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = MongoHelper.BuildFieldFilter(Table.PKField, ids);
        DeleteResult result = await CreateBsonCollection(false).DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbModelProvider<DbModel>.AsQueryable(string? routing)
    {
        var collection = CreateCollection(isReadonly: true);
        return new MongoQueryable<DbModel>(collection, filterBuilder: null, routing);
    }
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbModelProvider<DbModel>.AsUpdatable(string? routing)
    {
        var collection = CreateCollection(isReadonly: false);
        return new MongoUpdatable<DbModel>(collection, filterBuilder: null, routing);
    }
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbModelProvider<DbModel>.AsDeletable(string? routing)
    {
        var collection = CreateCollection(isReadonly: false);
        return new MongoDeletable<DbModel>(collection, filterBuilder: null, routing);
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 获取数据库服务器地址
    /// </summary>
    /// <param name="isReadonly">是否为只读</param>
    /// <returns></returns>
    protected DbServerDescriptor GetServer(bool isReadonly)
        => DbManager.GetServer(DbServer, isReadonly, null2Error: true)!;
    /// <summary>
    /// 构建实体的BsonDocument连接对象
    /// </summary>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置；默认读操作</param>
    /// <returns></returns>
    protected IMongoCollection<BsonDocument> CreateBsonCollection(bool isReadonly = true)
        => MongoHelper.CreateCollection<BsonDocument>(GetServer(isReadonly), Table.Name);
    /// <summary>
    /// 创建数据实体的链接对象
    /// </summary>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置；默认读操作</param>
    /// <returns></returns>
    protected IMongoCollection<DbModel> CreateCollection(bool isReadonly = true)
        => MongoHelper.CreateCollection<DbModel>(GetServer(isReadonly));
    #endregion
}
