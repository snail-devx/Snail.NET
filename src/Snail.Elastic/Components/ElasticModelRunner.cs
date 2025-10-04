using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Snail.Abstractions;
using Snail.Abstractions.Database;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Extensions;
using Snail.Abstractions.Dependency.Extensions;
using Snail.Abstractions.Logging;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using static Snail.Elastic.Utils.ElasticHelper;

namespace Snail.Elastic.Components
{
    /// <summary>
    /// Elastic的数据实体执行器；负责执行具体的Elastic操作 <br />
    ///     1、封装Elastic部分操作，供其他组件调用： <br />
    ///         <see cref="ElasticProvider{DbModel}"/> <br />
    ///         <see cref="ElasticDeletable{DbModel}"/>、<see cref="ElasticUpdatable{DbModel}"/>、<see cref="ElasticQueryable{DbModel}"/>公共调用
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <remarks>后期考虑写得更规范一些，抽取出接口出来</remarks>
    public class ElasticModelRunner<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 实体数据表信息
        /// </summary>
        public static readonly DbModelTable Table;
        /// <summary>
        /// 数据字段映射；key为属性名，value为字段信息
        /// </summary>
        public static readonly IReadOnlyDictionary<string, DbModelField> FieldMap;
        /// <summary>
        /// 数据库实体的JSON序列化配置
        /// </summary>
        protected static readonly JsonSerializerSettings JsonSetting = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            ContractResolver = new DbModelJsonResolver<DbModel>(),
        };

        /// <summary>
        /// 数据库管理器
        /// </summary>
        protected readonly IDbManager DbManager;
        /// <summary>
        /// 数据库服务器配置选项
        /// </summary>
        protected readonly IDbServerOptions Server;
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger Logger;
        #endregion

        #region 构造方法
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static ElasticModelRunner()
        {
            Table = DbModelHelper.GetTable<DbModel>();
            FieldMap = Table.GetFieldMap();
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app">应用程序实例</param>
        /// <param name="server">数据库服务器配置选项</param>
        public ElasticModelRunner(IApplication app, IDbServerOptions server)
        {
            ThrowIfNull(app);
            DbManager = app.ResolveRequired<IDbManager>();
            Server = ThrowIfNull(server);
            Logger = app.ResolveRequired<ILogger>();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取数据库服务器地址
        /// </summary>
        /// <param name="isReadonly">是否为只读</param>
        /// <returns></returns>
        public DbServerDescriptor GetServer(bool isReadonly)
            => DbManager.GetServer(Server, isReadonly, null2Error: true)!;
        /// <summary>
        /// 发送Elastic的Post请求；给外部使用，做更细化的定制
        /// </summary>
        /// <param name="isReadonly">是否是只读操作</param>
        /// <param name="title">请求标题</param>
        /// <param name="api">API地址</param>
        /// <param name="postData">Post提交数据</param>
        /// <returns>操作结果字符串</returns>
        public Task<string?> Post(bool isReadonly, string title, string api, string postData)
        {
            ThrowIfNullOrEmpty(api);
            ThrowIfNullOrEmpty(postData);
            DbServerDescriptor server = GetServer(isReadonly);
            return PostString(title, server, api, postData, Logger);
        }

        /// <summary>
        /// 查询【_search】操作；查询+聚合逻辑
        /// </summary>
        /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
        /// <param name="search">查询实体</param>
        /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
        /// <returns></returns>
        public async Task<ElasticSearchResult<DbModel>> Search(string? routing, ElasticSearchModel search, IList<string>? urlParams = null)
        {
            //  组装api地址
            string api = BuildAction(Table.Name, "_search", routing, urlParams);
            //  发送Post请求；记录一下查询参数
            DbServerDescriptor server = GetServer(isReadonly: true);
            string ret = (await PostString("Search", server, api, search.AsJson(), Logger, logResult: false))!;
            ElasticSearchResult<DbModel> mr = ret.As<ElasticSearchResult<DbModel>>(JsonSetting);
            return mr;
        }
        /// <summary>
        /// 基于查询条件，遍历数据
        /// </summary>
        /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
        /// <param name="query">查询条件</param>
        /// <param name="each">遍历方法；每次分页遍历取到的数据信息</param>
        /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
        /// <returns>遍历到的数据量</returns>
        public async Task<long> ForEachDatas(string? routing, ElasticQueryModel query, Func<ElasticSearchResult<DbModel>, Task> each, IList<string>? urlParams = null)
        {
            //  分组遍历符合当前查询条件的所有数据，并交给each委托做处理
            ThrowIfNull(query);
            ThrowIfNull(each);
            //  构建基础参数
            string api = BuildAction(Table.Name, "_search", routing, urlParams);
            ElasticSearchModel search = new ElasticSearchModel()
            {
                Souce = bool.FalseString,
                Query = query,
                Size = 1000,
                Sort = [new ElasticSortModel(Table.PKField.Name, isAsc: false)]
            };
            //  遍历发送api
            long total = 0;
            bool needLoop = true;
            while (needLoop)
            {
                //  查数据，取不到直接中断
                needLoop = false;
                ElasticSearchResult<DbModel> ret = await Search(routing, search, urlParams);
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
        /// <param name="isSave">是保存还是创建；true（批量报错），false（批量创建）</param>
        /// <param name="models"></param>
        /// <returns></returns>
        public string BuildModelBulks(bool isSave, IList<DbModel> models)
        {
            ThrowIfNullOrEmpty(models);
            ThrowIfHasNull(models!);
            //  注意json序列化时，把不需要的属性剔除掉；可以构建在delete；因为json序列化不认DbFieldAttribute自定义属性
            StringBuilder bulks = new StringBuilder();
            foreach (DbModel model in models)
            {
                //  构建操作信息：取路由，若启用了路由则强制非空
                string? routing = (model as IDbRouting)?.GetRouting();
                if (Table.Routing == true && string.IsNullOrEmpty(routing) == true)
                {
                    routing = $"{nameof(DbModel)}已启用Routing；但实例GetRouting()值为空，请排查IDbModelRouting.GetRouting返回值。{model.AsJson()}";
                    throw new ArgumentException(routing);
                }
                //  解析主键Id值
                string? idValue;
                {
                    object? pValue = Table.PKField.Property.GetValue(model);
                    idValue = DbModelHelper.BuildFieldValue(pValue, Table.PKField)?.ToString();
                }
                if (string.IsNullOrEmpty(idValue) == true)
                {
                    idValue = $"分析出来的主键Id值为空，Model：{model.AsJson()}";
                    throw new ApplicationException(idValue);
                }
                ElasticBulkModel bulk = isSave == true
                    ? new ElasticBulkSaveModel() { Id = idValue, Routing = routing, }
                    : new ElasticBulkCreateModel() { Id = idValue, Routing = routing };
                bulks.AppendLine(bulk.AsJson());
                //  实体信息转JSON
                bulks.AppendLine(model.AsJson(JsonSetting));
            }
            return bulks.ToString();
        }
        /// <summary>
        /// 批量【_bulk】操作：如批量更新、删除、插入等
        /// </summary>
        /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
        /// <param name="bulk">要进行批量提交的内容</param>
        /// <param name="refresh">批量操作数据是否立即刷新生效；可选值：true，false，wait_for(默认)</param>
        /// <returns></returns>
        public async Task<ElasticBulkResult> Bulk(string? routing, string bulk, string? refresh = null)
        {
            if (bulk?.Any() != true)
            {
                string msg = $"执行批量操作时，{nameof(bulk)}不能为空";
                throw new ArgumentNullException(nameof(bulk), msg);
            }
            //  api路径处理
            List<string> urlParams = new List<string>() { $"refresh={Default(refresh, "wait_for")}" };
            string api = BuildAction(Table.Name, "_bulk", routing, urlParams);
            //  发送api
            DbServerDescriptor server = GetServer(isReadonly: false);
            string? ret = await PostString("Bulk", server, api, bulk, Logger, logPost: false, logResult: false);
            //  处理批量操作结果：转不成功则报错
            ElasticBulkResult br = ret?.As<ElasticBulkResult>()
                ?? throw new ApplicationException("获取[_bulk]操作结果失败；返回null");
            //      针对操作Error做一些处理，如【routing_missing_exception】，这类强制报错，但【document_missing_exception】忽略掉
            List<string> exs = new List<string>();
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
        /// <typeparam name="IdType">主键字段类型</typeparam>
        /// <param name="routing">分片路由值；无则传null</param>
        /// <param name="idRoutingMap">要更新的数据主键Id：routing字典。key为主键id，value为对应的routing值，若为null则使用<paramref name="routing"/></param>
        /// <param name="updates">要更新的字段值；key为<typeparamref name="DbModel"/>的属性名，value为对应值</param>
        /// <returns>更新的文档数量</returns>
        /// <remarks>1、若es中索引强制约束了routing配置，则必须确保每个id数据都能有routing值</remarks>
        public async Task<long> Updates<IdType>(string? routing, IDictionary<IdType, string?> idRoutingMap, IDictionary<string, object?> updates)
            where IdType : notnull
        {
            //  验证updates，剔除不ignore标记的字段
            ThrowIfNullOrEmpty(idRoutingMap);
            ThrowIfNullOrEmpty(updates);
            routing = Default(routing, null);
            foreach (var kv in updates)
            {
                var field = Table.Fields.FirstOrDefault(field => field.Property.Name == kv.Key);
                if (field == null)
                {
                    string msg = $"Updates：未找到{nameof(updates)}中[{kv.Key}]对应的数据字段信息；{typeof(DbModel)}";
                    throw new KeyNotFoundException(msg);
                }
            }
            //  构建批量提交
            /*
             *      其实这种方式不是特别好，每个id构建一个bulk，对post数据量随着ids增大，不友好；
             *      但要使用_update_by_query，则需要配合脚本使用，更不通用，且es对同时编译script数量有限制
             */
            StringBuilder bulks = new StringBuilder();
            //      构建更新数据：需要把属性名转换成字段名
            string update;
            {
                IDictionary<string, object?> jsonMap = new Dictionary<string, object?>();
                foreach (var kv in updates)
                {
                    if (FieldMap.TryGetValue(kv.Key, out DbModelField? field) == false)
                    {
                        string msg = $"{typeof(ElasticModelRunner<>)}.{nameof(Updates)}：{nameof(updates)}中[{kv.Key}]无数据字段信息；{typeof(DbModel)}";
                        throw new KeyNotFoundException(msg);
                    }
                    jsonMap[field.Name] = kv.Value;
                }
                update = new Dictionary<string, object>
                {
                    ["doc"] = jsonMap
                }.AsJson();
            }
            //      遍历更新
            foreach (var (id, idRouting) in idRoutingMap)
            {
                //  若Table强制约束了需要routing值，则需要验证出来做处理
                string? tmpStr = Default(idRouting, routing);
                if (Table.Routing == true && tmpStr == null)
                {
                    tmpStr = $"[{nameof(DbModel)}]强制[Routing]值，当前更新routing为空。id:{id},routing:{routing},idRouting:{idRouting}";
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
            ElasticBulkResult ret = await Bulk(routing: null, bulks.ToString());
            return ret.Items?.Count(item => item.Update != null && item.Update.Error == null) ?? 0;
        }
        /// <summary>
        /// 根据query条件删除数据
        /// </summary>
        /// <param name="routing">路由名；为null则不在API上追加 routing={routing}参数</param>
        /// <param name="query">查询条件</param>
        /// <param name="urlParams">url上附带的参数数组，格式 key=value。内部自带refresh和conflicts</param>
        /// <returns></returns>
        public async Task<long> DeleteByQuery(string? routing, ElasticQueryModel query, List<string>? urlParams = null)
        {
            //  组装api地址
            urlParams ??= new List<string>();
            urlParams.Add("refresh=true");
            //      尝试解决版本冲突 409问题：Response status code does not indicate success: 409 (Conflict)
            urlParams.Add("conflicts=proceed");
            string api = BuildAction(Table.Name, "_delete_by_query", routing, urlParams);
            //  发送post请求
            DbServerDescriptor server = GetServer(isReadonly: false);
            string tmpStr = new ElasticSearchModel() { Query = query }.AsJson();
            tmpStr = (await PostString("DeleteByQuery", server, api, tmpStr, Logger))!;
            return JToken.Parse(tmpStr)?["deleted"]?.Value<long>() ?? 0;
        }
        #endregion
    }
}
