using System.Linq.Expressions;
using Snail.Abstractions.Database.Attributes;


namespace Snail.Abstractions.Database.Interfaces
{
    /// <summary>
    /// 数据库更新接口
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public interface IDbUpdatable<DbModel> where DbModel : class
    {
        /// <summary>
        /// 查询条件<br />
        ///     1、多次调用时内部进行and合并
        /// </summary>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        /// <remarks>Where条件生效规则和<see cref="IDbQueryable{DbModel}.Where(Expression{Func{DbModel, bool}})"/>保持一致</remarks>
        IDbUpdatable<DbModel> Where(Expression<Func<DbModel, bool>> predicate);

        /// <summary>
        /// 设置字段值<br />
        ///     1、多次调用按顺序合并<br />
        ///     2、仅针对更新操作生效<br />
        /// </summary>
        /// <typeparam name="TField">返回字段类型</typeparam>
        /// <param name="fieldLambda">字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
        /// <param name="value">字段值</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbUpdatable<DbModel> Set<TField>(Expression<Func<DbModel, TField>> fieldLambda, TField value);
        /// <summary>
        /// 批量设置字段值<br />
        ///     1、多次调用按顺序合并<br />
        ///     2、仅针对更新操作生效<br />
        /// </summary>
        /// <param name="data">字段值字典。key为DbModel属性名，vlaue为字段值</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbUpdatable<DbModel> Set(IDictionary<string, object?> data);

        /// <summary>
        /// 执行更新操作
        /// </summary>
        /// <remarks>禁止无条件更新、禁止无更新字段</remarks>
        /// <returns>更新数据条数</returns>
        Task<long> Update();
    }
}
