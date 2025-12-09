using Snail.Utilities.Common.Extensions;
using System.Runtime.Serialization;

namespace Snail.Elastic.DataModels;

/// <summary>
/// Elastic的Search实体 
///     进行ES数据检索_SearchAPI时相关实体
/// </summary>
[Serializable]
public sealed class ElasticSearchModel : ISerializable
{
    /// <summary>
    /// 返回结构是否需要_source值
    /// </summary>
    public string? Souce { set; get; }

    /// <summary>
    /// 查询条件配置
    /// </summary>
    public ElasticQueryModel? Query { set; get; }
    /// <summary>
    /// 后置过滤条件
    ///     1、不会影响聚合数据范围
    /// </summary>
    public ElasticQueryModel? PostFilter { set; get; }

    /// <summary>
    /// 排序配置
    /// </summary>
    public List<ElasticSortModel>? Sort { set; get; }

    /// <summary>
    /// 上一页的排序值，用于分页使用
    /// </summary>
    public List<object>? SearchAfter { set; get; }
    /// <summary>
    /// 从第几条开始取数据 <br />
    ///     1、等效于老系统的StartIndex值； <br />
    ///     2、有性能问题：不推荐使用，仅为了兼容老系统而存在；新系统使用<see cref="SearchAfter"/>替代 <br />
    ///     3、当传入了<see cref="SearchAfter"/>后，此值强制无效 <br />
    /// </summary>
    [Obsolete("仅为兼容老系统存在，新系统请使用“SearchAfter”")]
    public int? From { set; get; }
    /// <summary>
    /// 本次查询取多少条数据
    /// </summary>
    public int? Size { set; get; }

    /// <summary>
    /// 聚合统计配置
    /// </summary>
    public ElasticAggContainer? Aggs { set; get; }

    #region ISerializable 自定义序列化，满足null干掉的逻辑
    /// <summary>
    /// JSON序列化
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.TryAddValue("_source", Souce)
            .TryAddValue("query", Query)
            .TryAddValue("post_filter", PostFilter)
            .TryAddValue("sort", Sort)
            .TryAddValue("search_after", SearchAfter)
            .TryAddValue("size", Size)
            .TryAddValue("aggs", Aggs)
#pragma warning disable CS0618
            .TryAddValue("from", From)
#pragma warning restore CS0618
            ;
    }
    #endregion
}
