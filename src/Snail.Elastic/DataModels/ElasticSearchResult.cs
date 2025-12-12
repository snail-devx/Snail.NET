using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Snail.Elastic.DataModels;

/// <summary>
/// ElasticSearch数据搜索结果
/// <para>不用完全把结果属性整理出来，仅限实际需要的 </para>
/// </summary>
/// <typeparam name="SourceModel">索引数据的Source实体类型</typeparam>
public sealed class ElasticSearchResult<SourceModel> where SourceModel : class
{
    #region 属性变量
    /// <summary>
    /// 搜索花费时长
    /// </summary>
    [JsonProperty(PropertyName = "took")]
    public int Took { set; get; }

    /// <summary>
    /// 搜索是否超时
    /// </summary>
    [JsonProperty(PropertyName = "timed_out")]
    public bool Timedout { set; get; }

    /// <summary>
    /// 搜索结果数据
    /// </summary>
    [JsonProperty(PropertyName = "hits")]
    public ElasticSearchHitsModel? Hits { set; get; }

    /// <summary>
    /// 聚合查询结果
    ///     这个内部结构不统一，这里不做统一格式要求
    /// </summary>
    [JsonProperty(PropertyName = "aggregations")]
    public JObject? Aggregations { set; get; }
    #endregion

    #region 内部类
    /// <summary>
    /// 数据搜索命中数据结果集
    /// </summary>
    public sealed class ElasticSearchHitsModel
    {
        /* 超过1w条数据后，这里给的结果.value始终为1w，通过relation区分。
         *      给值不精确，直接忽略不用
        /// <summary>
        /// 搜索结果数据条数
        ///     符合查询条件的所有数据条数，排除分页影响
        /// </summary>
        [JsonProperty(PropertyName = "total")]
        public ElasticSearchTotalModel Total { set; get; }
        */

        /// <summary>
        /// 命中数据的最大分数
        /// </summary>
        [JsonProperty(PropertyName = "max_score")]
        public double? MaxScore { set; get; }

        /// <summary>
        /// 命中数据集合
        /// </summary>
        [JsonProperty(PropertyName = "hits")]
        public List<ElasticSearchHitModel>? Hits { set; get; }
    }

    /// <summary>
    /// 数据搜索结果合计实体
    /// </summary>
    public sealed class ElasticSearchTotalModel
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public long Value { set; get; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty(PropertyName = "relation")]
        public string? Relation { set; get; }
    }

    /// <summary>
    /// 数据搜索命中的单条数据信息实体
    /// </summary>
    public sealed class ElasticSearchHitModel
    {
        /* 先不放开
        /// <summary>
        /// 操作类型
        /// </summary>
        [JsonProperty("_type")]
        public String Type { set; get; }
         */
        /// <summary>
        /// 索引名
        /// </summary>
        [JsonProperty("_index")]
        public required string Index { set; get; }

        /// <summary>
        /// 数据Id
        /// </summary>
        [JsonProperty(PropertyName = "_id")]
        public required string Id { set; get; }

        /// <summary>
        /// 数据得分
        /// </summary>
        [JsonProperty(PropertyName = "_score")]
        public double? Score { set; get; }

        /// <summary>
        /// 数据路由
        /// </summary>
        [JsonProperty(PropertyName = "_routing")]
        public string? Routing { set; get; }

        /// <summary>
        /// 构建索引时的源数据
        /// </summary>
        [JsonProperty(PropertyName = "_source")]
        public SourceModel? Source { set; get; }

        /// <summary>
        /// 数据排序值：用于进行search_after查询使用
        /// </summary>
        [JsonProperty(PropertyName = "sort")]
        public List<object>? Sort { set; get; }
        /// <summary>
        /// 数据命中的Name查询条件集合
        /// </summary>
        [JsonProperty(PropertyName = "matched_queries")]
        public List<string>? MatchedQueries { set; get; }
    }
    #endregion
}
