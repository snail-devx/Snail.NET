using Snail.Database.Components;
using Snail.SqlCore.Interfaces;

namespace Snail.SqlCore.Components
{
    /// <summary>
    /// <see cref="IDbDeletable{DbModel}"/>接口的关系型数据库实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class SqlDeletable<DbModel> : DbDeletable<DbModel>, IDbDeletable<DbModel> where DbModel : class
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
        public SqlDeletable(ISqlDbRunner runner, SqlFilterBuilder<DbModel> builder, string? routing)
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
        public override async Task<long> Delete()
        {
            string sql = FilterBuilder.BuildFilter(Filters, out IDictionary<string, object> param);
            ThrowIfNullOrEmpty(sql, $"基于过滤条件组装sql条件语句为空：{Filters}");
            sql = Runner.BuildDeleteSql(sql);
            long count = await Runner.RunDbActionAsync(con => con.ExecuteAsync(sql, param), false, false);
            return count;
        }
        #endregion
    }
}
