using Newtonsoft.Json;

namespace Snail.Elastic.DataModels;

/// <summary>
/// ElasticSearch索引批量请求操作结果 <br />
///     不用完全把结果属性整理出来，仅限实际需要的
/// </summary>
public sealed class ElasticBulkResult
{
    /// <summary>
    /// 耗费时间
    /// </summary>
    [JsonProperty("took")]
    public int Took { set; get; }

    /// <summary>
    /// 是否发生错误
    /// </summary>
    [JsonProperty("errors")]

    public bool Errors { set; get; }

    /// <summary>
    /// 操作结果项目
    /// </summary>
    [JsonProperty("items")]
    public required List<BulkItemModel> Items { set; get; }

    #region 内部类
    /// <summary>
    /// 批量操作项实体
    /// </summary>
    public sealed class BulkItemModel
    {
        /// <summary>
        /// 创建索引操作结果
        /// </summary>
        [JsonProperty("create")]
        public BulkItemDetailModel? Create { set; get; }
        /// <summary>
        /// 保存（创建/替换）索引操作结果
        /// </summary>
        [JsonProperty("index")]
        public BulkItemDetailModel? Index { set; get; }
        /// <summary>
        /// 更新索引操作结果
        /// </summary>
        [JsonProperty("update")]
        public BulkItemDetailModel? Update { set; get; }
        /// <summary>
        /// 删除索引操作结果
        /// </summary>
        [JsonProperty("delete")]
        public BulkItemDetailModel? Delete { set; get; }
    }
    /// <summary>
    /// 批量操作结果结果详细信息
    /// </summary>
    public sealed class BulkItemDetailModel
    {
        /// <summary>
        /// 数据主键Id
        /// </summary>
        [JsonProperty("_id")]
        public required string Id { set; get; }
        /// <summary>
        /// 操作状态
        /// </summary>
        [JsonProperty("status")]
        public int Status { set; get; }
        /// <summary>
        /// 操作结果
        /// </summary>
        [JsonProperty("result")]
        public string? Result { set; get; }

        /// <summary>
        /// 发生错误时的详细信息
        /// </summary>
        [JsonProperty("error")]
        public BulkItemDetailErrorModel? Error { set; get; }

        /* 先不放开
         /// <summary>
         /// 索引名
         /// </summary>
         [JsonProperty("_index")]
         public string Index { set; get; }
         /// <summary>
         /// 操作类型
         /// </summary>
         [JsonProperty("_type")]
         public string Type { set; get; }
          */
    }

    /// <summary>
    /// 发生错误的原因实体
    /// </summary>
    public sealed class BulkItemDetailErrorModel
    {
        /// <summary>
        /// 错误类型
        /// </summary>
        [JsonProperty("type")]
        public required string Type { set; get; }
        /// <summary>
        /// 错误原因
        /// </summary>
        [JsonProperty("reason")]
        public required string Reason { set; get; }

        /*先不放开
        /// <summary>
        /// 索引唯一Id
        /// </summary>
        public string index_uuid { set; get; }
        /// <summary>
        /// 分片
        /// </summary>
        public string shard { set; get; }
        /// <summary>
        /// 索引名
        /// </summary>
        public string index { set; get; }*/
    }
    #endregion
}
