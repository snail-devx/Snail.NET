using Snail.Database.Components;
using Snail.Elastic.DataModels;

namespace Snail.Elastic.Components
{
    /// <summary>
    /// <see cref="IDbUpdatable{DbModel}"/>接口的Elastic实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class ElasticUpdatable<DbModel> : DbUpdatable<DbModel>, IDbUpdatable<DbModel> where DbModel : class
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
        public ElasticUpdatable(ElasticModelRunner<DbModel> runner, ElasticFilterBuilder<DbModel>? builder, string? routing)
            : base(routing)
        {
            Runner = ThrowIfNull(runner);
            FilterBuilder = builder ?? ElasticFilterBuilder<DbModel>.Default;
        }
        #endregion

        #region IDbUpdatable：部分交给【DbUpdatable】做默认实现
        /// <summary>
        /// 执行更新操作
        /// </summary>
        /// <remarks>禁止无条件更新、禁止无更新字段</remarks>
        /// <returns>更新数据条数</returns>
        public async override Task<long> Update()
        {
            /**
             * 需要注意，elastic自身未实现直接where条件增量更新，需要考虑使用查询条件遍历所有id数据，然后进行批量id更新逻辑
             * 还有一种方式，就是update_by_query的script脚本逻辑，但在大批量环境下时，会导致es动态编译script耗费性能，且可能超过script数量限制
             */
            //  遍历符合条件数据，遍历时，不需要具体的数据，仅需返回Source字段值即可
            ElasticQueryModel query = FilterBuilder.BuildFilter(Filters);
            List<string> urlParams = ["_source=false"];
            long total = await Runner.ForEachDatas(Routing, query, async ret =>
            {
                //  取到id和routing值
                IDictionary<string, string?> idRoutingMap = ret.Hits!.Hits!.ToDictionary(hit => hit.Id, hit => hit.Routing)!;
                await Runner.Updates(Routing, idRoutingMap, Updates);
            }, urlParams);
            return total;
        }
        #endregion
    }
}
