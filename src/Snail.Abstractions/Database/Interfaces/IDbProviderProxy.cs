using Snail.Abstractions.Database.Attributes;

namespace Snail.Abstractions.Database.Interfaces;

/// <summary>
/// 接口约束：数据库提供者代理
/// </summary>
public interface IDbProviderProxy : IDbProvider
{
    /// <summary>
    /// 被代理的数据库提供程序
    /// </summary>
    public IDbProvider Provider { get; }

    #region IDbProvider，做默认实现
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns></returns>
    Task<bool> IDbProvider.Insert<DbModel>(params IList<DbModel> models)
          => Provider.Insert(models);
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    Task<bool> IDbProvider.Save<DbModel>(params IList<DbModel> models)
        => Provider.Save(models);
    /// <summary>
    /// 基于主键id值加载数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要加载的数据主键id值集合</param>
    /// <returns>数据实体集合</returns>
    /// <remarks>
    /// <para> 1、不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsQueryable(string)"/>方法</para>
    /// </remarks>
    Task<IList<DbModel>> IDbProvider.Load<DbModel, IdType>(IList<IdType> ids)
        => Provider.Load<DbModel, IdType>(ids);
    /// <summary>
    /// 基于主键id值更新数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要更新的数据主键id值集合</param>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <returns>更新的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
    Task<long> IDbProvider.Update<DbModel, IdType>(IList<IdType> ids, IDictionary<string, object?> updates)
        => Provider.Update<DbModel, IdType>(ids, updates);
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsDeletable(string)"/>方法</remarks>
    Task<long> IDbProvider.Delete<DbModel, IdType>(params IList<IdType> ids)
        => Provider.Delete<DbModel, IdType>(ids);

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbProvider.AsQueryable<DbModel>(string? routing)
        => Provider.AsQueryable<DbModel>(routing);
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbProvider.AsUpdatable<DbModel>(string? routing)
        => Provider.AsUpdatable<DbModel>(routing);
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbProvider.AsDeletable<DbModel>(string? routing)
        => Provider.AsDeletable<DbModel>(routing);
    #endregion
}