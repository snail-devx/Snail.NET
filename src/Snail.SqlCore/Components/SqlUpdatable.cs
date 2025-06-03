using Snail.Database.Components;
using Snail.SqlCore.Interfaces;

namespace Snail.SqlCore.Components
{
    /// <summary>
    /// <see cref="IDbUpdatable{DbModel}"/>接口的关系型数据库实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class SqlUpdatable<DbModel> : DbUpdatable<DbModel>, IDbUpdatable<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 过滤条件构建器
        /// </summary>
        protected readonly SqlFilterBuilder<DbModel> FilterBuilder;
        /// <summary>
        /// sql数据库运行器
        /// </summary>
        protected readonly ISqlDbRunner Runner;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="runner">运行器</param>
        /// <param name="builder">过滤条件构建器</param>
        /// <param name="routing">路由信息</param>
        public SqlUpdatable(ISqlDbRunner runner, SqlFilterBuilder<DbModel> builder, string? routing)
            : base(routing)
        {
            Runner = ThrowIfNull(runner);
            FilterBuilder = ThrowIfNull(builder);
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
            //  构建更新的where和set
            string sql = FilterBuilder.BuildFilter(Filters, out IDictionary<string, object> whereParam);
            sql = Runner.BuildUpdateSql(Updates, sql, whereParam, out IDictionary<string, object> param);
            //  执行操作：暂时先不用事务
            long count = await Runner.RunDbActionAsync(con => con.ExecuteAsync(sql, param), false, true);
            return count;
        }
        #endregion
    }
}
