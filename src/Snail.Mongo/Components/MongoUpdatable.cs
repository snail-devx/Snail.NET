using Snail.Database.Components;
using Snail.Mongo.Utils;

namespace Snail.Mongo.Components;

/// <summary>
/// <see cref="IDbUpdatable{DbModel}"/>接口的Mongo实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public class MongoUpdatable<DbModel> : DbUpdatable<DbModel>, IDbUpdatable<DbModel> where DbModel : class
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
    public MongoUpdatable(IMongoCollection<DbModel> collection, MongoFilterBuilder<DbModel>? filterBuilder, string? routing)
        : base(routing)
    {
        DbCollection = ThrowIfNull(collection);
        FilterBuilder = filterBuilder ?? MongoFilterBuilder<DbModel>.Default;
    }
    #endregion

    #region IDbUpdatable：部分交给【DbUpdatable】做默认实现
    /// <summary>
    /// 执行更新操作
    /// </summary>
    /// <remarks>禁止无条件更新、禁止无更新字段</remarks>
    /// <returns>更新数据条数</returns>
    public override async Task<long> Update()
    {
        FilterDefinition<DbModel> filter = FilterBuilder.BuildFilter(Filters);
        UpdateDefinition<DbModel> update = MongoHelper.BuildUpdate<DbModel>(Updates);
        UpdateResult resut = await DbCollection.UpdateManyAsync(filter, update);
        return resut.ModifiedCount;
    }
    #endregion
}
