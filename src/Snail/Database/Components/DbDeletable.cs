using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using System.Linq.Expressions;

namespace Snail.Database.Components;

/// <summary>
/// <see cref="IDbDeletable{DbModel}"/>接口实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public abstract class DbDeletable<DbModel> : IDbDeletable<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 路由分片
    /// </summary>
    protected readonly string? Routing;
    /// <summary>
    /// 过滤条件
    /// </summary>
    protected readonly List<Expression<Func<DbModel, bool>>> Filters = new();
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    /// <param name="routing"></param>
    public DbDeletable(string? routing)
    {
        Routing = Default(routing, defaultStr: null);
    }
    #endregion

    #region IDbDeletable
    /// <summary>
    /// 查询条件
    ///     1、多次调用时内部进行and合并
    /// </summary>
    /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
    /// <returns>数据库查询对象，方便链式调用</returns>
    /// <remarks>Where条件生效规则和<see cref="IDbQueryable{DbModel}.Where(Expression{Func{DbModel, bool}})"/>保持一致</remarks>
    IDbDeletable<DbModel> IDbDeletable<DbModel>.Where(Expression<Func<DbModel, bool>> predicate)
    {
        ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// 执行删除操作
    /// </summary>
    /// <remarks>禁止无条件删除</remarks>
    /// <returns>删除数据条数</returns>
    public abstract Task<long> Delete();
    #endregion

    #region 继承方法
    #endregion

    #region 内部方法
    #endregion

    #region 私有方法
    #endregion
}
