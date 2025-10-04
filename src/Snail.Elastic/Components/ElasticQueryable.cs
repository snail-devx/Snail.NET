using Newtonsoft.Json.Linq;
using Snail.Abstractions.Database.DataModels;
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
    /// <see cref="IDbQueryable{DbModel}"/>接口的Elastic实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class ElasticQueryable<DbModel> : DbQueryable<DbModel>, IDbQueryable<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// Elasti运行器
        /// </summary>
        protected readonly ElasticModelRunner<DbModel> Runner;
        /// <summary>
        /// 过滤条件构建器
        /// </summary>
        protected readonly ElasticFilterBuilder<DbModel> FilterBuilder;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="runner">运行器</param>
        /// <param name="builder">过滤条件构建器；为null则使用默认的<see cref="ElasticFilterBuilder{DbModel}"/></param>
        /// <param name="routing">路由信息</param>
        public ElasticQueryable(ElasticModelRunner<DbModel> runner, ElasticFilterBuilder<DbModel>? builder, string? routing)
            : base(routing)
        {
            Runner = ThrowIfNull(runner);
            FilterBuilder = builder ?? ElasticFilterBuilder<DbModel>.Default;
        }
        #endregion

        #region IDbQueryable：部分交给DbQueryable做默认实现
        /// <summary>
        /// 符合Where条件的【所有数据】条数
        /// </summary>
        /// <remarks>仅使用Where条件做查询；Skip、Take、Order等失效</remarks>
        /// <returns>符合条件的数据条数</returns>
        public override async Task<long> Count()
        {
            //  _发送count查询请求
            ElasticSearchModel search = BuildFilter();
            string api = BuildAction(ElasticModelRunner<DbModel>.Table.Name, "_count", Routing);
            string? ret = await Runner.Post(isReadonly: true, title: "CountAsync", api, search.AsJson());
            //  解析count值
            /*  { "count": 1, "_shards": {...} } */
            long? count = JToken.Parse(ret ?? "{}")["count"]?.Value<long>();
            return count ?? 0;
        }
        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>存在返回true；否则返回false</returns>
        public override async Task<bool> Any()
        {
            //  构建全量查询条件，包括排序分页等；但强制仅取1条数据，且只要_id值，仅用作判断存在性使用
            IList<string> urlParam = ["filter_path=hits.hits._id"];
            ElasticSearchResult<DbModel> ret = await ToSearch(needSelect: false, search => search.Size = 1, urlParam);
            return ret?.Hits?.Hits?.Count == 1;
        }

        /// <summary>
        /// 获取符合条件的第一条数据；
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据实体；无则返回默认值</returns>
        public override async Task<DbModel?> FirstOrDefault()
        {
            //  构建全量查询条件，包括分页排序等；但要强制仅取1条数据，且仅要source值，其他的不要
            IList<string> urlParams = [PARAM_OnlySource];
            ElasticSearchResult<DbModel> ret = await ToSearch(needSelect: true, init: search => search.Size = 1, urlParams);
            return ret?.Hits?.Hits?.FirstOrDefault()?.Source;
        }
        /// <summary>
        /// 获取符合筛选条件+分页的所有数据<br />
        ///     1、禁止无条件查询
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据库实体集合</returns>
        public override async Task<IList<DbModel>> ToList()
        {
            //  构建全量查询条件，包括分页排序等；且仅要source值，其他的不要
            List<string> urlParams = [PARAM_OnlySource];
            ElasticSearchResult<DbModel> ret = await ToSearch(needSelect: true, init: null, urlParams: urlParams);
            // 无数据，给默认空集合，和mongodb数据库保持一致
            return ret?.ToSource()?.ToList() ?? [];
        }
        /// <summary>
        /// 获取符合筛选条件+分页的查询结果<br />
        ///     1、支持LastSortKey逻辑
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns></returns>
        public async override Task<DbQueryResult<DbModel>> ToResult()
        {
            //  构建全量查询，得到最后一条数据的sort值，作为lastSortKey值
            ElasticSearchResult<DbModel> ret = await ToSearch();
            DbQueryResult<DbModel> qt = new DbQueryResult<DbModel>(ret?.ToSource()?.ToArray());
            //  整理lastSortKey：有值取最后一个
            qt.LastSortKey = qt.Page > 0
                ? ret!.Hits!.Hits!.Last().Sort?.AsJson()?.AsBase64Encode()
                : null;
            return qt;
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 构建Elastic查询过滤条件 <br />
        ///     1、仅使用<see cref="IDbQueryable{DbModel}.Where" />方法传入条件构建
        /// </summary>
        /// <returns></returns>
        protected ElasticSearchModel BuildFilter()
        {
            ElasticQueryModel query = FilterBuilder.BuildFilter(Filters);
            return new ElasticSearchModel()
            {
                Query = query
            };
        }
        /// <summary>
        /// 构建搜索条件
        /// </summary>
        /// <param name="needSource">查询是否返回source值；如仅做聚合查询，则可以不用返回source以优化性能</param>
        /// <returns></returns>
        protected ElasticSearchModel BuildSearch(bool needSource = true)
        {
            //  1、构建查询过滤条件
            ElasticSearchModel search = BuildFilter();
            search.Souce = needSource == false ? bool.FalseString : null;
            //  2、排序：强制加上_id排序，避免分页时出现唯一性判断摇摆的问题
            search.Sort = GetSorts(ElasticModelRunner<DbModel>.Table.PKField.Property.Name)
                  .Select(kv =>
                  {
                      if (ElasticModelRunner<DbModel>.FieldMap.TryGetValue(kv.Key, out DbModelField? field) == false)
                      {
                          string msg = $"{nameof(BuildSearch)}：未找到排序中[{kv.Key}]对应的数据字段信息；{typeof(DbModel)}";
                          throw new KeyNotFoundException(msg);
                      }
                      return new ElasticSortModel(field.Name, kv.Value);
                  })
                  .ToList();
            //  3、构建分页逻辑：若size未填写，暂时按照默认值处理；不强制为0。ES默认1000的情况，这里做一些强制限制，避免性能问题
            //      使用LastSortKey模式分页：Size值不能超过MaxSize值，null给出默认值
            if (LastSortKey?.Length > 0)
            {
                search.SearchAfter = LastSortKey.AsBase64Decode().As<List<object>>();
                search.Size = Take ?? MAX_Size;
                if (search.Size > MAX_Size)
                {
                    string msg = $"LastSortKey模式下，Take值不能超过{MAX_Size}。Take:{Take}";
                    throw new ArgumentException(msg);
                }
            }
            //      from+size模式：和值不能超过MaxSize值
            else
            {
#pragma warning disable CS0618
                search.From = Skip ?? 0;
                search.Size = Take ?? (MAX_Size - search.From);
                if ((search.From + search.Size) > MAX_Size)
                {
                    string msg = $"Skip+Take模式下，Skip+Take不能超过1w。Skip:{Skip}; Take:{Take}";
                    throw new ArgumentException(msg);
                }
#pragma  warning restore CS0618
            }
            //      若明确指定了不需要取数据，则强制不要Source数据
            search.Souce = search.Size == 0 ? bool.FalseString : null;

            return search;
        }

        /// <summary>
        /// 执行Search操作
        /// </summary>
        /// <param name="needSelect">是否需要进行字段选择；<see cref="IDbQueryable{DbModel}.Select"/>、<see cref="IDbQueryable{DbModel}.UnSelect{TField}"/>是否生效</param>
        /// <param name="init">search查询条件初始化：在构建完成后，外部做特定初始化工作</param>
        /// <param name="urlParams">url上附带的参数数组，格式 key=value</param>
        /// <returns></returns>
        protected Task<ElasticSearchResult<DbModel>> ToSearch(bool needSelect = true, Action<ElasticSearchModel>? init = null, IList<string>? urlParams = null)
        {
            ElasticSearchModel search = BuildSearch();
            init?.Invoke(search);
            //  构建排除字段和例外字段的url参数 _source_excludes、_source_includes
            //      Selects： 将selects字段值转换成实际的数据库字段名称
            if (needSelect == true && Selects.Count > 0)
            {
                DbModelTable table = DbModelHelper.GetTable<DbModel>();
                string selectFields = Selects
                    .Select(pName => ElasticModelRunner<DbModel>.FieldMap.GetValueOrDefault(pName)?.Name)
                    .Where(field => field != null)
                    .AsString(",");
                urlParams ??= [];
                urlParams.Add($"_source_includes={selectFields}");
            }
            //  发送查询API逻辑
            return Runner.Search(Routing, search, urlParams);
        }
        #endregion
    }
}
