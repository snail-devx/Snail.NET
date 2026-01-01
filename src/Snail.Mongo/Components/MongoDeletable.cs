using Snail.Database.Components;

namespace Snail.Mongo.Components;

/// <summary>
/// <see cref="IDbDeletable{DbModel}"/>接口的Mongo实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public class MongoDeletable<DbModel> : DbDeletable<DbModel>, IDbDeletable<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 数据库表对象
    /// </summary>
    protected readonly IMongoCollection<DbModel> DbCollection;
    /// <summary>
    /// 过滤条件构建器
    /// </summary>
    protected readonly MongoFilterBuilder<DbModel> FilterBuilder;
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    /// <param name="collection">数据表</param>
    /// <param name="filterBuilder">过滤条件构建器</param>
    /// <param name="routing">路由</param>
    public MongoDeletable(IMongoCollection<DbModel> collection, MongoFilterBuilder<DbModel>? filterBuilder, string? routing)
        : base(routing)
    {
        DbCollection = ThrowIfNull(collection);
        FilterBuilder = filterBuilder ?? MongoFilterBuilder<DbModel>.Default;
    }
    #endregion

    #region IDbDeletable：部分交给【DbDeletable】做默认实现
    /// <summary>
    /// 执行删除操作
    /// </summary>
    /// <remarks>禁止无条件删除</remarks>
    /// <returns>删除数据条数</returns>
    public override async Task<long> Delete()
    {
        FilterDefinition<DbModel> filter = FilterBuilder.BuildFilter(Filters);
        DeleteResult result = await DbCollection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    #endregion
}
