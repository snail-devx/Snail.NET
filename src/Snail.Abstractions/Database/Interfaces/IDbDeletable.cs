using System.Linq.Expressions;
using Snail.Abstractions.Database.Attributes;

namespace Snail.Abstractions.Database.Interfaces
{
    /// <summary>
    /// 数据库删除接口
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public interface IDbDeletable<DbModel> where DbModel : class
    {
        /// <summary>
        /// 查询条件
        ///     1、多次调用时内部进行and合并
        /// </summary>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        /// <remarks>Where条件生效规则和<see cref="IDbQueryable{DbModel}.Where(Expression{Func{DbModel, bool}})"/>保持一致</remarks>
        IDbDeletable<DbModel> Where(Expression<Func<DbModel, bool>> predicate);

        /// <summary>
        /// 执行删除操作
        /// </summary>
        /// <remarks>禁止无条件删除</remarks>
        /// <returns>删除数据条数</returns>
        Task<long> Delete();
    }
}
