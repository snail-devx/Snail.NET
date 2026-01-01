using Newtonsoft.Json.Linq;
using Snail.Abstractions;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Dependency.Attributes;
using Snail.Abstractions.Dependency.Enumerations;
using Snail.Abstractions.Dependency.Extensions;
using Snail.Abstractions.Logging;
using Snail.Database.Components;
using Snail.Elastic.Components;
using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;
using System.Text;
using static Snail.Database.Components.DbModelProxy;
using static Snail.Elastic.Utils.ElasticHelper;

namespace Snail.Elastic;
/// <summary>
/// <see cref="IDbProvider"/>的Elastic实现 
/// <para>1、前期先使用HTTP发送，后续看情况引入Elastic官方组件 </para>
/// <para>2、强制【瞬时】生命周期，避免不同服务器之间操作实例问题，使用【ElasticSearch】作为依赖注入key值 </para>
/// </summary>
[Component<IDbProvider>(Key = nameof(DbType.ElasticSearch), Lifetime = LifetimeType.Transient)]
public class ElasticProvider : DbProvider, IDbProvider
{
    #region 属性变量
    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger Logger;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">数据库服务器配置选项</param>
    public ElasticProvider(IApplication app, IDbServerOptions server)
        : base(app, server)
    {
        Logger = app.ResolveRequired<ILogger>();
    }
    #endregion

    #region IDbModelProvider
    /// <summary>
    /// 插入数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">数据对象集合；可变参数，至少传入一个</param>
    /// <returns></returns>
    async Task<bool> IDbProvider.Insert<DbModel>(params IList<DbModel> models)
    {
        /*
         * 现在先使用批量操作方法；后续判断models个数，若为一个，则使用单个插入逻辑；
         * 执行批量操作：routing名在bulk中分析出来，这里强制传null：若存在有错误的结果，则算失败了
         */
        string bulk = BuildModelBulks(isSave: false, models);
        ElasticBulkResult ret = await Bulk<DbModel>(routing: null, bulk);
        bool isSuccess = ret.Items?.Any(item => item.Create == null || item.Create.Error != null) == false;
        return isSuccess;
    }
    /// <summary>
    /// 保存数据：存在覆盖，不存在插入
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="models">要保存的数据实体对象集合</param>
    /// <returns>保存成功返回true；否则返回false</returns>
    async Task<bool> IDbProvider.Save<DbModel>(params IList<DbModel> models)
    {
        /*
         * 先使用批量操作方法，后续在考虑使用单个的
         * 执行批量操作：routing名在bulk中分析出来，这里强制传null：只要存在一个不无错结果就算成功
         */
        string bulk = BuildModelBulks(isSave: true, models);
        ElasticBulkResult ret = await Bulk<DbModel>(routing: null, bulk);
        bool isSuccess = ret?.Items?.Any(item => item.Index != null && item.Index.Error == null) == true;
        return isSuccess;
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
        ElasticSearchResult<DbModel> ret = await Search<DbModel>(routing: null, search, urlParams: [PARAM_OnlySource]);
        //      无数据，给默认空集合，和mongodb数据库保持一致
        var result = ret.ToSource()?.ToList() ?? [];
        return result;
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
        ThrowIfNullOrEmpty(ids);
        //  若需要table强制了需要routing，则先查一下；否则直接更新
        if (GetProxy<DbModel>().Table.Routing == true)
        {
            ElasticQueryModel query = new ElasticIdsQueryModel(ids.Select(id => id!.ToString()!).ToArray());
            List<string> urlParams = ["_source=false"];
            long count = await ForEachDatas<DbModel>(routing: null, query, async ret =>
            {
                IDictionary<string, string?> idRoutingMap = ret.Hits!.Hits!.ToDictionary(hit => hit.Id!, hit => hit.Routing);
                await Updates<DbModel, string>(routing: null, idRoutingMap, updates);
            }, urlParams);
            return count;
        }
        //  不强制routing时，直接更新
        else
        {
            Dictionary<IdType, string?> idRoutingMap = [];
            foreach (IdType id in ids)
            {
                idRoutingMap[id] = null;
            }
            return await Updates<DbModel, IdType>(routing: null, idRoutingMap, updates);
        }
    }
    /// <summary>
    /// 基于主键id值删除数据，此接口仅支持单主键
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键的数据类型，确保和数据实体标记的主键字段类型一致</typeparam>
    /// <param name="ids">要删除的数据主键id值集合</param>
    /// <returns>删除的数据条数</returns>
    /// <remarks>不支持指定数据分片路由；若需要，请使用<see cref="IDbProvider.AsDeletable(string)"/>方法</remarks>
    Task<long> IDbProvider.Delete<DbModel, IdType>(params IList<IdType> ids)
    {
        /**
         * 先走批量逻辑，后续判断ids数量，若为一个则直出
         * 此接口routing强制null，若需要分片删除，使用AsDeletable()
         */
        ThrowIfNullOrEmpty(ids);
        ElasticIdsQueryModel query = new ElasticIdsQueryModel(ids.Select(id => id.ToString()!).ToArray());
        return DeleteByQuery<DbModel>(routing: null, query);
    }

    /// <summary>
    /// 构建数据库查询接口；用于完成符合条件数据的查询、排序、分页等操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现查询分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbQueryable<DbModel> IDbProvider.AsQueryable<DbModel>(string? routing)
        => new ElasticQueryable<DbModel>(provider: this, builder: null, routing);
    /// <summary>
    /// 构建数据库更新接口；用于完成符合条件数据的更新操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现更新分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbUpdatable<DbModel> IDbProvider.AsUpdatable<DbModel>(string? routing)
        => new ElasticUpdatable<DbModel>(provider: this, builder: null, routing);
    /// <summary>
    /// 构建数据库删除接口；用于完成符合条件数据的删除操作
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由Key；实现删除分片数据逻辑，具体看数据库是否支持；数据表是否配置分片</param>
    /// <returns>接口实例</returns>
    IDbDeletable<DbModel> IDbProvider.AsDeletable<DbModel>(string? routing)
        => new ElasticDeletable<DbModel>(provider: this, builder: null, routing);
    #endregion

    #region 内部方法
    /// <summary>
    /// 获取数据库服务器地址
    /// </summary>
    /// <param name="isReadonly">是否为只读</param>
    /// <returns></returns>
    internal DbServerDescriptor GetServer(bool isReadonly)
        => DbManager.GetServer(DbServer, isReadonly, null2Error: true)!;
    /// <summary>
    /// 发送Elastic的Post请求；给外部使用，做更细化的定制
    /// </summary>
    /// <param name="isReadonly">是否是只读操作</param>
    /// <param name="title">请求标题</param>
    /// <param name="api">API地址</param>
    /// <param name="postData">Post提交数据</param>
    /// <returns>操作结果字符串</returns>
    internal Task<string?> Post(bool isReadonly, string title, string api, string postData)
    {
        ThrowIfNullOrEmpty(api);
        ThrowIfNullOrEmpty(postData);
        DbServerDescriptor server = GetServer(isReadonly);
        return PostString(title, server, api, postData, Logger);
    }

    /// <summary>
    /// 查询【_search】操作；查询+聚合逻辑
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
    /// <param name="search">查询实体</param>
    /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
    /// <returns></returns>
    internal async Task<ElasticSearchResult<DbModel>> Search<DbModel>(string? routing, ElasticSearchModel search, IList<string>? urlParams = null)
        where DbModel : class
    {
        DbModelProxy proxy = GetProxy<DbModel>();
        //  组装api地址
        string api = BuildAction(proxy.TableName, "_search", routing, urlParams);
        //  发送Post请求；记录一下查询参数
        DbServerDescriptor server = GetServer(isReadonly: true);
        string ret = (await PostString("Search", server, api, search.AsJson(), Logger, logResult: false))!;
        ElasticSearchResult<DbModel> mr = ret.As<ElasticSearchResult<DbModel>>(proxy.JsonSetting);
        return mr;
    }
    /// <summary>
    /// 基于查询条件，遍历数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
    /// <param name="query">查询条件</param>
    /// <param name="each">遍历方法；每次分页遍历取到的数据信息</param>
    /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
    /// <returns>遍历到的数据量</returns>
    internal async Task<long> ForEachDatas<DbModel>(string? routing, ElasticQueryModel query, Func<ElasticSearchResult<DbModel>, Task> each, IList<string>? urlParams)
        where DbModel : class
    {
        //  分组遍历符合当前查询条件的所有数据，并交给each委托做处理
        ThrowIfNull(query);
        ThrowIfNull(each);
        //  构建基础参数
        ElasticSearchModel search = new ElasticSearchModel()
        {
            Souce = bool.FalseString,
            Query = query,
            Size = 1000,
            Sort = [new ElasticSortModel(GetProxy<DbModel>().PKField.Name, isAsc: false)]
        };
        //  遍历发送api
        long total = 0;
        bool needLoop = true;
        while (needLoop)
        {
            //  查数据，取不到直接中断
            ElasticSearchResult<DbModel> ret = await Search<DbModel>(routing, search, urlParams);
            if (ret?.IsSourceAny() != true)
            {
                break;
            }
            //  分析是否需要继续分页遍历：filter_path=hits.hits.sort 若没返回sort，则强制报错，否则会出现死循环
            search.SearchAfter = ret.Hits!.Hits!.Last().Sort;
            ThrowIfNull(search.SearchAfter, "ForEach后，sort值为空；请排查urlParams是否返回了sort字段");
            needLoop = ret.Hits.Hits!.Count == search.Size;
            total += ret.Hits.Hits.Count;
            //  执行遍历委托，并等待执行完成
            each(ret)?.Wait();
        }
        return total;
    }

    /// <summary>
    /// 构建实体的批量bulk操作内容
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="isSave">是保存还是创建；true（批量报错），false（批量创建）</param>
    /// <param name="models"></param>
    /// <returns></returns>
    internal string BuildModelBulks<DbModel>(bool isSave, IList<DbModel> models) where DbModel : class
    {
        ThrowIfNullOrEmpty(models);
        ThrowIfHasNull(models!);
        DbModelProxy proxy = GetProxy<DbModel>();
        //  注意json序列化时，把不需要的属性剔除掉；可以构建在delete；因为json序列化不认DbFieldAttribute自定义属性
        StringBuilder bulks = new StringBuilder();
        foreach (DbModel model in models)
        {
            //  构建操作信息：取路由，若启用了路由则强制非空
            string? routing = (model as IDbRouting)?.GetRouting();
            if (proxy.Table.Routing == true && string.IsNullOrEmpty(routing) == true)
            {
                routing = $"{nameof(DbModel)}已启用Routing；但实例GetRouting()值为空，请排查IDbModelRouting.GetRouting返回值。{model.AsJson()}";
                throw new ArgumentException(routing);
            }
            //  解析主键Id值
            string? idValue = GetDbValue(model, proxy.PKField)?.ToString();
            if (IsNullOrEmpty(idValue) == true)
            {
                idValue = $"分析出来的主键Id值为空，Model：{model.AsJson()}";
                throw new ApplicationException(idValue);
            }
            ElasticBulkModel bulk = isSave == true
                ? new ElasticBulkSaveModel() { Id = idValue, Routing = routing, }
                : new ElasticBulkCreateModel() { Id = idValue, Routing = routing };
            bulks.AppendLine(bulk.AsJson());
            //  实体信息转JSON
            bulks.AppendLine(model.AsJson(proxy.JsonSetting));
        }
        return bulks.ToString();
    }
    /// <summary>
    /// 批量【_bulk】操作：如批量更新、删除、插入等
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
    /// <param name="bulk">要进行批量提交的内容</param>
    /// <param name="refresh">批量操作数据是否立即刷新生效；可选值：true，false，wait_for(默认)</param>
    /// <returns></returns>
    internal async Task<ElasticBulkResult> Bulk<DbModel>(string? routing, string bulk, string? refresh = null) where DbModel : class
    {
        if (bulk?.Any() != true)
        {
            string msg = $"执行批量操作时，{nameof(bulk)}不能为空";
            throw new ArgumentNullException(nameof(bulk), msg);
        }
        //  api路径处理
        List<string> urlParams = new List<string>() { $"refresh={Default(refresh, "wait_for")}" };
        string api = BuildAction(GetProxy<DbModel>().TableName, "_bulk", routing, urlParams);
        //  发送api
        DbServerDescriptor server = GetServer(isReadonly: false);
        string? ret = await PostString("Bulk", server, api, bulk, Logger, logPost: false, logResult: false);
        //  处理批量操作结果：转不成功则报错
        ElasticBulkResult br = ret?.As<ElasticBulkResult>()
            ?? throw new ApplicationException("获取[_bulk]操作结果失败；返回null");
        //      针对操作Error做一些处理，如【routing_missing_exception】，这类强制报错，但【document_missing_exception】忽略掉
        List<string> exs = [];
        foreach (var item in br.Items)
        {
            var detail = item.Create ?? item.Update ?? item.Delete ?? item.Index;
            //      Error为空，或者【document_missing_exception】错误，先忽略掉
            if (detail != null && detail.Error != null && detail.Error.Type != "document_missing_exception")
            {
                exs.Add(detail.AsJson());
            }
        }
        if (exs.Count > 0)
        {
            string msg = $"ES执行bulk发生错误：\r\n{exs.AsString("\r\n")}";
            throw new ApplicationException(msg);
        }
        return br;
    }

    /// <summary>
    /// 基于主键Id增量更新数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <typeparam name="IdType">主键字段类型</typeparam>
    /// <param name="routing">分片路由值；无则传null</param>
    /// <param name="idRoutingMap">要更新的数据主键Id：routing字典。key为主键id，value为对应的routing值，若为null则使用<paramref name="routing"/></param>
    /// <param name="updates">更新的数据信息；key为属性名，value为更新值</param>
    /// <returns>更新的文档数量</returns>
    /// <remarks>1、若es中索引强制约束了routing配置，则必须确保每个id数据都能有routing值</remarks>
    internal async Task<long> Updates<DbModel, IdType>(string? routing, IDictionary<IdType, string?> idRoutingMap, IDictionary<string, object?> updates)
       where DbModel : class where IdType : notnull
    {
        //  验证updates；构建更新数据：需要把属性名转换成字段名
        ThrowIfNullOrEmpty(idRoutingMap);
        ThrowIfNullOrEmpty(updates);
        DbModelProxy proxy = GetProxy<DbModel>();
        routing = Default(routing, null);
        string update;
        {
            Dictionary<string, object?> jsonMap = [];
            foreach (var kv in updates)
            {
                DbModelField field = proxy.GetField(kv.Key, $"Updates属性[{kv.Key}]");
                jsonMap[field.Name] = kv.Value;
            }
            update = new Dictionary<string, object>
            {
                ["doc"] = jsonMap
            }.AsJson();
        }
        //  构建批量提交
        /*
         *      其实这种方式不是特别好，每个id构建一个bulk，对post数据量随着ids增大，不友好；
         *      但要使用_update_by_query，则需要配合脚本使用，更不通用，且es对同时编译script数量有限制
         */
        //      遍历更新
        StringBuilder bulks = new StringBuilder();
        foreach (var (id, idRouting) in idRoutingMap)
        {
            //  若Table强制约束了需要routing值，则需要验证出来做处理
            string? tmpStr = Default(idRouting, routing);
            if (proxy.Table.Routing == true && tmpStr == null)
            {
                tmpStr = $"[{proxy.Table.Type.Name}]强制[Routing]值，当前更新routing为空。id:{id},routing:{routing},idRouting:{idRouting}";
                throw new ApplicationException(tmpStr);
            }
            //  构建批量操作
            tmpStr = new ElasticBulkUpdateModel()
            {
                Id = id.ToString()!,
                ConflictRetry = 10,
                Routing = tmpStr,
            }.AsJson();
            bulks.AppendLine(tmpStr);
            bulks.AppendLine(update);
        }
        ElasticBulkResult ret = await Bulk<DbModel>(routing: null, bulks.ToString());
        return ret.Items?.Count(item => item.Update != null && item.Update.Error == null) ?? 0;
    }
    /// <summary>
    /// 根据query条件删除数据
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
    /// <param name="query">查询条件</param>
    /// <param name="urlParams">url上附带的参数数组，格式 key=value。内部自带refresh和conflicts</param>
    /// <returns></returns>
    internal async Task<long> DeleteByQuery<DbModel>(string? routing, ElasticQueryModel query, List<string>? urlParams = null)
        where DbModel : class
    {
        //  组装api地址
        urlParams ??= [];
        urlParams.Add("refresh=true");
        //      尝试解决版本冲突 409问题：Response status code does not indicate success: 409 (Conflict)
        urlParams.Add("conflicts=proceed");
        string api = BuildAction(GetProxy<DbModel>().TableName, "_delete_by_query", routing, urlParams);
        //  发送post请求
        DbServerDescriptor dbServer = GetServer(isReadonly: false);
        string tmpStr = new ElasticSearchModel() { Query = query }.AsJson();
        tmpStr = (await PostString("DeleteByQuery", dbServer, api, tmpStr, Logger))!;
        return JToken.Parse(tmpStr)?["deleted"]?.Value<long>() ?? 0;
    }
    #endregion
}