using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.Interfaces;
using Snail.Abstractions.Distribution;
using Snail.Abstractions.Identity.Extensions;
using Snail.Aspect.Distribution.Enumerations;
using Snail.Database.Attributes;
using Snail.Database.Interfaces;
using System.Diagnostics.CodeAnalysis;
using static Snail.Database.Components.DbModelProxy;

namespace Snail.Database.Components;
/// <summary>
/// 数据库提供程序的缓存代理
/// <para>1、对save、insert、update方法操作数据进行缓存</para>
/// <para>2、分析Save等传入的DbModel类型标注的<see cref="DbCacheAttribute"/>特性，从而确定如何缓存数据</para>
/// </summary>
/// <remarks>不作为依赖注入组件自动注册，外部根据需要进行选举构建</remarks>
public class DbProviderCacheProxy : IDbProviderProxy
{
    #region 属性变量
    /// <summary>
    /// 应用程序
    /// </summary>
    protected readonly IApplication App;
    /// <summary>
    /// 被代理的数据库提供程序
    /// </summary>
    protected IDbProvider Provider { get; }
    /// <summary>
    /// 缓存访问器
    /// </summary>
    protected readonly ICacher Cacher;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public DbProviderCacheProxy(IApplication app, IDbProvider provider, ICacher cacher)
    {
        App = ThrowIfNull(app);
        Provider = ThrowIfNull(provider);
        Cacher = ThrowIfNull(cacher);
    }
    #endregion

    #region IDbProviderProxy 
    /// <summary>
    /// 被代理的数据库提供程序
    /// </summary>
    IDbProvider IDbProviderProxy.Provider => Provider;
    #endregion

    #region IDbProvider 重写部分方法，实现缓存功能
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns></returns>
    async Task<bool> IDbProvider.Insert<DbModel>(params IList<DbModel> models)
    {
        bool bValue = await Provider.Insert(models);
        if (bValue == true && IsNullOrEmpty(models) == false && NeedCache<DbModel>(out DbModelProxy? proxy) == true)
        {
            await SaveCache(proxy, models);
        }
        return bValue;
    }
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    async Task<bool> IDbProvider.Save<DbModel>(params IList<DbModel> models)
    {
        bool bValue = await Provider.Save(models);
        if (bValue == true && IsNullOrEmpty(models) == false && NeedCache<DbModel>(out DbModelProxy? proxy) == true)
        {
            await SaveCache(proxy, models);
        }
        return bValue;
    }
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
    async Task<IList<DbModel>> IDbProvider.Load<DbModel, IdType>(IList<IdType> ids)
    {
        DbModelProxy? proxy = null;
        List<DbModel> models = [];
        //  先从缓存中加载
        if (IsNullOrEmpty(ids) == false && NeedCache<DbModel>(out proxy) == true)
        {
            IList<DbModel> cacheModels = await LoadCache<DbModel, IdType>(proxy, ids);
            if (IsNullOrEmpty(cacheModels) == false)
            {
                models.AddRange(cacheModels);
                ids = ids.Except(cacheModels.Select(model => (IdType)ExtractDbFieldValue(proxy.PKField, model)!)).ToList();
            }
        }
        //  从数据库取数据，并加入缓存中
        if (IsNullOrEmpty(ids) == false)
        {
            IList<DbModel> dbModels = await Provider.Load<DbModel, IdType>(ids);
            if (IsNullOrEmpty(dbModels) == false)
            {
                models.AddRange(dbModels);
                if (proxy!.CacheOptions != null)
                {
                    await SaveCache(proxy, dbModels);
                }
            }
        }

        return models;
    }
    /// <summary>
    /// 基于主键id值更新数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要更新的数据主键id值集合</param>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <returns>更新的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsUpdatable(string)"/>方法</remarks>
    async Task<long> IDbProvider.Update<DbModel, IdType>(IList<IdType> ids, IDictionary<string, object?> updates)
    {
        /* 更新成功后，重新基于ids取一下数据，然后保存到缓存中 */
        long count = await Provider.Update<DbModel, IdType>(ids, updates);
        if (count > 0 && IsNullOrEmpty(ids) == false && NeedCache<DbModel>(out DbModelProxy? proxy) == true)
        {
            IList<DbModel> models = await Provider.Load<DbModel, IdType>(ids);
            await SaveCache(proxy, models);
        }
        return count;
    }
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsDeletable(string)"/>方法</remarks>
    async Task<long> IDbProvider.Delete<DbModel, IdType>(params IList<IdType> ids)
    {
        long count = await Provider.Delete<DbModel, IdType>(ids);
        if (IsNullOrEmpty(ids) == false && NeedCache<DbModel>(out DbModelProxy? proxy) == true)
        {
            await DeleteCache<DbModel, IdType>(proxy, ids);
        }
        return count;
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 获取缓存分析器
    /// <para>1、指定了解析器，DI注入失败则则报错</para>
    /// </summary>
    /// <param name="attr"></param>
    /// <returns></returns>
    protected IDbCacheAnalyzer GetAnalyzer(DbCacheAttribute attr)
        => IsNullOrEmpty(attr.Analyzer) ? DbCacheAnalyzer.DEFAULT : App.ResolveRequired<IDbCacheAnalyzer>(attr.Analyzer);
    /// <summary>
    /// 是否需要缓存
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="proxy"></param>
    /// <returns></returns>
    protected bool NeedCache<DbModel>([NotNullWhen(true)] out DbModelProxy? proxy) where DbModel : class
    {
        proxy = GetProxy<DbModel>();
        return proxy.CacheOptions != null;
    }

    /// <summary>
    /// 保存缓存
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="models"></param>
    /// <remarks>1、执行此方法时，<see cref="DbModelProxy.CacheOptions"/>已强制验证非null</remarks>
    /// <returns></returns>
    protected virtual async Task SaveCache<DbModel>(DbModelProxy proxy, IList<DbModel> models) where DbModel : class
    {
        IDbCacheAnalyzer analyzer = GetAnalyzer(proxy.CacheOptions!);
        Dictionary<string, DbModel> map = new();
        foreach (DbModel model in models)
        {
            string idValue = analyzer.GetDataKey(proxy, model, proxy.CacheOptions!.DataKeyPrefix);
            map[idValue] = model;
        }
        switch (proxy.CacheOptions!.Type)
        {
            case CacheType.ObjectCache:
                await Cacher.AddObject(map, proxy.CacheOptions.ExpireSeconds);
                break;
            case CacheType.HashCache:
                string masterKey = analyzer.GetMasterKey<DbModel>(proxy, proxy.CacheOptions.MasterKey);
                await Cacher.AddHash(masterKey, map, proxy.CacheOptions.ExpireSeconds);
                break;
            default:
                throw new NotSupportedException($"不支持的缓存类型:{proxy.CacheOptions.Type.ToString()}");
        }
    }
    /// <summary>
    /// 加载缓存
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <typeparam name="IdType"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="ids"></param>
    /// <remarks>1、执行此方法时，<see cref="DbModelProxy.CacheOptions"/>已强制验证非null</remarks>
    /// <returns></returns>
    protected virtual async Task<IList<DbModel>> LoadCache<DbModel, IdType>(DbModelProxy proxy, IList<IdType> ids) where DbModel : class where IdType : notnull
    {
        IDbCacheAnalyzer analyzer = GetAnalyzer(proxy.CacheOptions!);
        List<string> dataKeys = ids.Select(id => analyzer.GetDataKey<DbModel, IdType>(proxy, id, proxy.CacheOptions!.DataKeyPrefix)).ToList();
        switch (proxy.CacheOptions!.Type)
        {
            case CacheType.ObjectCache:
                return await Cacher.GetObject<DbModel>(dataKeys);
            case CacheType.HashCache:
                string masterKey = analyzer.GetMasterKey<DbModel>(proxy, proxy.CacheOptions.MasterKey);
                return await Cacher.GetHash<DbModel>(masterKey, dataKeys);
            default:
                throw new NotSupportedException($"不支持的缓存类型:{proxy.CacheOptions.Type.ToString()}");
        }
    }
    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <typeparam name="DbModel"></typeparam>
    /// <typeparam name="IdType"></typeparam>
    /// <param name="proxy"></param>
    /// <param name="ids"></param>
    /// <remarks>执行此方法时，<see cref="DbModelProxy.CacheOptions"/>已强制验证非null</remarks>
    /// <returns></returns>
    protected virtual async Task DeleteCache<DbModel, IdType>(DbModelProxy proxy, IList<IdType> ids) where DbModel : class where IdType : notnull
    {
        IDbCacheAnalyzer? analyzer = GetAnalyzer(proxy.CacheOptions!);
        List<string> dataKeys = ids.Select(id => analyzer.GetDataKey<DbModel, IdType>(proxy, id, proxy.CacheOptions!.DataKeyPrefix)).ToList();
        switch (proxy.CacheOptions!.Type)
        {
            case CacheType.ObjectCache:
                await Cacher.RemoveObject<DbModel>(dataKeys);
                break;
            case CacheType.HashCache:
                string masterKey = analyzer.GetMasterKey<DbModel>(proxy, proxy.CacheOptions.MasterKey);
                await Cacher.RemoveHash<DbModel>(masterKey, dataKeys);
                break;
            default:
                throw new NotSupportedException($"不支持的缓存类型:{proxy.CacheOptions.Type.ToString()}");
        }
    }
    #endregion
}