using System.Linq.Expressions;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database.Extensions
{
    /// <summary>
    /// <see cref="IDbQueryable{DbModel}"/>扩展方法
    /// </summary>
    public static class DbQueryableExtensions
    {
        #region 公共方法
        /// <summary>
        /// 符合条件的数据条数
        /// </summary>
        /// <typeparam name="DbModel"></typeparam>
        /// <param name="query">数据库查询接口</param>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns></returns>
        public static Task<long> CountAsync<DbModel>(this IDbQueryable<DbModel> query, Expression<Func<DbModel, bool>> predicate)
            where DbModel : class
        {
            if (predicate != null)
            {
                query.Where(predicate);
            }
            return query.Count();
        }

        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <typeparam name="DbModel"></typeparam>
        /// <param name="query">数据库查询接口</param>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns>存在返回true；否则返回false</returns>
        public static Task<bool> AnyAsync<DbModel>(this IDbQueryable<DbModel> query, Expression<Func<DbModel, bool>> predicate)
            where DbModel : class
        {
            if (predicate != null)
            {
                query.Where(predicate);
            }
            return query.Any();
        }
        /// <summary>
        /// 获取符合条件的第一条数据
        /// </summary>
        /// <typeparam name="DbModel"></typeparam>
        /// <param name="query">数据库查询接口</param>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns></returns>
        public static Task<DbModel?> FirstOrDefaultAsync<DbModel>(this IDbQueryable<DbModel> query, Expression<Func<DbModel, bool>> predicate)
            where DbModel : class
        {
            if (predicate != null)
            {
                query.Where(predicate);
            }
            return query.FirstOrDefault();
        }
        #endregion
    }
}
