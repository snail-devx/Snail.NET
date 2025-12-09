using Snail.Database.Components;
using Snail.Elastic.DataModels;

namespace Snail.Elastic.Components;

/// <summary>
/// <see cref="IDbDeletable{DbModel}"/>接口的Elastic实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public class ElasticDeletable<DbModel> : DbDeletable<DbModel>, IDbDeletable<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// Elasti运行器
    /// </summary>
    protected readonly ElasticModelRunner<DbModel> Runner;
    /// <summary>
    /// 过滤条件构建器
    /// </summary>
    protected readonly ElasticFilterBuilder<DbModel> FilterBuilder;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="runner">运行器</param>
    /// <param name="builder">过滤条件构建器；为null则使用默认的<see cref="ElasticFilterBuilder{DbModel}"/></param>
    /// <param name="routing">路由信息</param>
    public ElasticDeletable(ElasticModelRunner<DbModel> runner, ElasticFilterBuilder<DbModel>? builder, string? routing)
        : base(routing)
    {
        Runner = ThrowIfNull(runner);
        FilterBuilder = builder ?? ElasticFilterBuilder<DbModel>.Default;
    }
    #endregion

    #region IDbDeletable：部分交给【DbDeletable】做默认实现
    /// <summary>
    /// 执行删除操作
    /// </summary>
    /// <remarks>禁止无条件删除</remarks>
    /// <returns>删除数据条数</returns>
    public async override Task<long> Delete()
    {
        ElasticQueryModel query = FilterBuilder.BuildFilter(Filters);
        return await Runner.DeleteByQuery(Routing, query);
    }
    #endregion
}
