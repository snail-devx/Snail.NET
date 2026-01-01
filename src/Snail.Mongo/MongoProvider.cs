using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.Database.Components;
using Snail.Mongo.Components;
using Snail.Mongo.Utils;
using static Snail.Database.Components.DbModelProxy;
using static Snail.Mongo.Utils.MongoHelper;

namespace Snail.Mongo;
/// <summary>
/// <see cref="IDbProvider"/>的MongoDB实现 
/// <para>1、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【MongoDB】作为依赖注入key值 </para>
/// </summary>
[Component<IDbProvider>(Key = nameof(DbType.MongoDB), Lifetime = LifetimeType.Transient)]
public class MongoProvider : DbProvider, IDbProvider
{
    #region 属性变量
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">数据库服务器配置选项</param>
    public MongoProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
    }
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
        IList<BsonDocument> documents = models.Select(BuildDocument).ToList();
        await CreateBsonCollection<DbModel>(false).InsertManyAsync(documents);
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
        //  将实体直接反序列化成Document，少了几次DbModel<->BsonDocument的转换，对性能友好
        IMongoCollection<BsonDocument> collection = CreateBsonCollection<DbModel>(false);
        List<BsonDocument> inserts = [];
        foreach (var model in models)
        {
            BsonDocument document = BuildDocument(model);
            BsonValue pkValue = document.GetValue(PK_FIELDNAME);
            BsonDocument tmpDocument = new(PK_FIELDNAME, pkValue);
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
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要加载的数据主键id值集合</param>
    /// <returns>数据实体集合</returns>
    /// <remarks>
    /// <para> 1、不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsQueryable(string)"/>方法</para>
    /// </remarks>
    async Task<IList<DbModel>> IDbProvider.Load<DbModel, IdType>(IList<IdType> ids)
    {
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = BuildFieldFilter(GetProxy<DbModel>().PKField, ids);
        IFindFluent<DbModel, DbModel> fluent = CreateCollection<DbModel>(isReadonly: true).Find(filter);
        IList<DbModel> list = await fluent.ToListAsync();
        return list ?? [];
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
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = BuildFieldFilter(GetProxy<DbModel>().PKField, ids);
        UpdateDefinition<DbModel> update = BuildUpdate<DbModel>(updates);
        UpdateResult result = await CreateCollection<DbModel>(false).UpdateManyAsync(filter, update);
        return result.ModifiedCount;
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
        ThrowIfNullOrEmpty(ids, "ids为null或者空数组");
        BsonDocument filter = BuildFieldFilter(GetProxy<DbModel>().PKField, ids);
        DeleteResult result = await CreateBsonCollection<DbModel>(false).DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbProvider.AsQueryable<DbModel>(string? routing) where DbModel : class
    {
        IMongoCollection<DbModel> collection = CreateCollection<DbModel>(isReadonly: true);
        return new MongoQueryable<DbModel>(collection, filterBuilder: null, routing);
    }
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbProvider.AsUpdatable<DbModel>(string? routing) where DbModel : class
    {
        IMongoCollection<DbModel> collection = CreateCollection<DbModel>(isReadonly: false);
        return new MongoUpdatable<DbModel>(collection, filterBuilder: null, routing);
    }
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbProvider.AsDeletable<DbModel>(string? routing) where DbModel : class
    {
        IMongoCollection<DbModel> collection = CreateCollection<DbModel>(isReadonly: false);
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
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置；默认读操作</param>
    /// <returns></returns>
    protected IMongoCollection<BsonDocument> CreateBsonCollection<DbModel>(bool isReadonly = true) where DbModel : class
    {
        TryRegisterClassMap<DbModel>();
        return MongoHelper.CreateCollection<BsonDocument>(GetServer(isReadonly), GetProxy<DbModel>().TableName);
    }
    /// <summary>
    /// 创建数据实体的链接对象
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置；默认读操作</param>
    /// <returns></returns>
    protected IMongoCollection<DbModel> CreateCollection<DbModel>(bool isReadonly = true) where DbModel : class
    {
        TryRegisterClassMap<DbModel>();
        return MongoHelper.CreateCollection<DbModel>(GetServer(isReadonly));
    }
    #endregion

    #region 内部方法
    #endregion

    #region 私有方法

    #endregion
}