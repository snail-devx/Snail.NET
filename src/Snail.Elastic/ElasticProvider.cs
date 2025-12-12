using Snail.Abstractions;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.Database.Components;
using Snail.Elastic.Components;
using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;
using static Snail.Elastic.Utils.ElasticHelper;

namespace Snail.Elastic;

/// <summary>
/// <see cref="IDbModelProvider{DbModel}"/>的Elastic实现 
/// <para>1、前期先使用HTTP发送，后续看情况引入Elastic官方组件 </para>
/// <para>2、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【ElasticSearch】作为依赖注入key值 </para>
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
[Component(From = typeof(IDbModelProvider<>), Key = nameof(DbType.ElasticSearch), Lifetime = LifetimeType.Transient)]
public class ElasticProvider<DbModel> : DbProvider, IDbModelProvider<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 数据库运行器
    /// </summary>
    private ElasticModelRunner<DbModel> _runner;
    #endregion

    #region 构造方法
    /// <summary>
    /// 静态构造方法
    /// </summary>
    static ElasticProvider()
    {
    }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">数据库服务器配置选项</param>
    public ElasticProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
        ThrowIfNull(app);
        ThrowIfNull(server);

        _runner = new ElasticModelRunner<DbModel>(app, server);
    }
    #endregion

    #region IDbModelProvider
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns>插入成功返回true；否则返回false</returns>
    async Task<bool> IDbModelProvider<DbModel>.Insert(IList<DbModel> models)
    {
        /*
         * 现在先使用批量操作方法；后续判断models个数，若为一个，则使用单个插入逻辑；
         * 执行批量操作：routing名在bulk中分析出来，这里强制传null：若存在有错误的结果，则算失败了
         */
        string bulk = _runner.BuildModelBulks(isSave: false, models);
        ElasticBulkResult ret = await _runner.Bulk(routing: null, bulk);
        bool isSuccess = ret.Items?.Any(item => item.Create == null || item.Create.Error != null) == false;
        return isSuccess;
    }
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    async Task<bool> IDbModelProvider<DbModel>.Save(IList<DbModel> models)
    {
        /*
         * 先使用批量操作方法，后续在考虑使用单个的
         * 执行批量操作：routing名在bulk中分析出来，这里强制传null：只要存在一个不无错结果就算成功
         */
        string bulk = _runner.BuildModelBulks(isSave: true, models);
        ElasticBulkResult ret = await _runner.Bulk(routing: null, bulk);
        bool isSuccess = ret?.Items?.Any(item => item.Index != null && item.Index.Error == null) == true;
        return isSuccess;
    }
    /// <summary>
    /// 基于主键id值加载数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要加载的数据主键id值集合</param>
    /// <returns>数据实体集合</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsQueryable(string)"/>方法</remarks>
    async Task<IList<DbModel>> IDbModelProvider<DbModel>.Load<IdType>(IList<IdType> ids)
    {
        /*
         * 先使用批量逻辑，后续考虑判断ids数量，若为单个则走直出
         * 构建ids in查询逻辑
         * routing强制null；需要使用的话，用AsQueryable方法
         */
        //  构建基于id的in查询语句
        ThrowIfNullOrEmpty(ids);
        ThrowIfTrue(ids.Count > MAX_Size, "Elastic禁止一次取超过1w的数据");
        string[] inIds = ids.Select(id => id!.ToString()!).ToArray();
        ElasticSearchModel search = new ElasticSearchModel() { Query = new ElasticIdsQueryModel(inIds) };
        //      Elastic默认会要求一次不能取超过1w的数据，这里也做一下同步限制
        search.Size = ids.Count;
        //  发送api取数据
        List<string> urlParams = [PARAM_OnlySource];
        ElasticSearchResult<DbModel> ret = await _runner.Search(routing: null, search, urlParams);
        //      无数据，给默认空集合，和mongodb数据库保持一致
        var result = ret.ToSource()?.ToList() ?? [];
        return result;
    }
    /// <summary>
    /// 基于主键id值更新数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="updates">要更新的数据；key为DbModel的属性名称，Value为具体值</param>
    /// <param name="ids">要更新的数据主键id值集合</param>
    /// <returns>更新的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsUpdatable(string)"/>方法</remarks>
    async Task<long> IDbModelProvider<DbModel>.Update<IdType>(IDictionary<string, object?> updates, IList<IdType> ids)
    {
        ThrowIfNullOrEmpty(ids);
        //  若需要table强制了需要routing，则先查一下；否则直接更新
        if (ElasticModelRunner<DbModel>.Table.Routing == true)
        {
            ElasticQueryModel query = new ElasticIdsQueryModel(ids.Select(id => id!.ToString()!).ToArray());
            List<string> urlParams = ["_source=false"];
            long count = await _runner.ForEachDatas(routing: null, query, async ret =>
            {
                IDictionary<string, string?> idRoutingMap = ret.Hits!.Hits!.ToDictionary(hit => hit.Id!, hit => hit.Routing);
                await _runner.Updates(routing: null, idRoutingMap, updates);
            }, urlParams);
            return count;
        }
        //  不强制routing时，直接更新
        else
        {
            IDictionary<IdType, string?> idRoutingMap = new Dictionary<IdType, string?>();
            foreach (IdType id in ids)
            {
                idRoutingMap[id] = null;
            }
            return await _runner.Updates(routing: null, idRoutingMap, updates);
        }
    }
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbModelProvider{DbModel}.AsDeletable(string)"/>方法</remarks>
    Task<long> IDbModelProvider<DbModel>.Delete<IdType>(params IList<IdType> ids)
    {
        /**
         * 先走批量逻辑，后续判断ids数量，若为一个则直出
         * 此接口routing强制null，若需要分片删除，使用AsDeletable()
         */
        ThrowIfNullOrEmpty(ids);
        ElasticIdsQueryModel query = new ElasticIdsQueryModel(ids.Select(id => id.ToString()!).ToArray());
        return _runner.DeleteByQuery(routing: null, query);
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbModelProvider<DbModel>.AsQueryable(string? routing)
        => new ElasticQueryable<DbModel>(_runner, builder: null, routing);
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbModelProvider<DbModel>.AsUpdatable(string? routing)
        => new ElasticUpdatable<DbModel>(_runner, builder: null, routing);
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbModelProvider<DbModel>.AsDeletable(string? routing)
         => new ElasticDeletable<DbModel>(_runner, builder: null, routing);
    #endregion
}
