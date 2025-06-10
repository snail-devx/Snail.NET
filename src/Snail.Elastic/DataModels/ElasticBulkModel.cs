using System.Runtime.Serialization;

/*  Elasti批量操作相关实体；仅内部使用    */
namespace Snail.Elastic.DataModels
{
    /// <summary>
    /// ES批量索引操作基类
    /// </summary>
    [Serializable]
    internal abstract class ElasticBulkModel : ISerializable
    {
        #region 属性变量
        /// <summary>
        /// 批量操作名
        /// </summary>
        private readonly string _action;

        /// <summary>
        /// 例外的JSON序列化值操作
        /// </summary>
        protected Action<Dictionary<string, string>>? AddJSONValue { init; get; }
        /// <summary>
        /// 要操作的索引主键Id；不可为空
        /// </summary>
        public required string Id { init; get; }

        /// <summary>
        /// 要操作的索引名
        /// </summary>
        public string? Index { init; get; }

        /// <summary>
        /// 要操作的索引路由
        /// </summary>
        public required string? Routing { init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="action"></param>
        public ElasticBulkModel(string action)
        {
            _action = ThrowIfNullOrEmpty(action);
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ThrowIfNullOrEmpty(_action);
            ThrowIfNullOrEmpty(Id);
            /*  json序列化值
                update = new
                {
                    _id = index.Id,
                    _index = index.IndexName,
                    routing = index.OrganizationId
                }
             */
            Dictionary<string, string> dict = new()
            {
                ["_id"] = Id
            };
            string? tmpStr = Default(Index, null);
            if (tmpStr != null)
            {
                dict["_index"] = tmpStr;
            }
            tmpStr = Default(Routing, null);
            if (tmpStr != null)
            {
                dict["routing"] = tmpStr;
            }
            AddJSONValue?.Invoke(dict);

            info.AddValue(_action, dict);
        }
        #endregion
    }
    /// <summary>
    /// 批量操作：创建索引
    /// </summary>
    [Serializable]
    internal sealed class ElasticBulkCreateModel : ElasticBulkModel, ISerializable
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public ElasticBulkCreateModel() : base("create")
        {
        }
    }
    /// <summary>
    /// 批量操作：保存（Index）搜索，创建一个新文档或者替换一个现有的文档
    /// </summary>
    [Serializable]
    internal sealed class ElasticBulkSaveModel : ElasticBulkModel, ISerializable
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public ElasticBulkSaveModel() : base("index")
        {
        }
    }
    /// <summary>
    /// 批量操作：更新索引
    /// </summary>
    [Serializable]
    internal sealed class ElasticBulkUpdateModel : ElasticBulkModel, ISerializable
    {
        /// <summary>
        /// 版本冲突时的重试次数，默认不重试
        /// </summary>
        public int? ConflictRetry { init; get; }

        /// <summary>
        /// 构造方法
        /// </summary>
        public ElasticBulkUpdateModel() : base("update")
        {
            //  额外的JSON序列化值：
            AddJSONValue = dict =>
            {
                if (ConflictRetry > 0)
                {
                    dict["retry_on_conflict"] = ConflictRetry.Value.ToString();
                }
            };
        }
    }
    /// <summary>
    /// 批量操作：创建索引
    /// </summary>
    [Serializable]
    internal sealed class ElasticBulkDeleteModel : ElasticBulkModel, ISerializable
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public ElasticBulkDeleteModel() : base("delete")
        {
        }
    }

    /// <summary>
    /// 批量操作：索引更新脚本实体
    ///     先仅支持id脚本所有
    /// </summary>
    [Serializable]
    internal sealed class ElasticBuildUpdateScriptModel : ISerializable
    {
        /// <summary>
        /// 要执行的脚本Id
        /// </summary>
        public string Id { private init; get; }

        /// <summary>
        /// 脚本更新时传入参数
        /// </summary>
        public object Params { private init; get; }

        /// <summary>
        /// Json序列化时，是否忽略外层的Script标签
        ///     为true时，序列化成{id,params}
        ///     为false时，序列化成{script:{id,params}}
        /// </summary>
        public bool JsonIgnoreScriptTag { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="id">要执行的脚本Id；不能为null</param>
        /// <param name="params">脚本更新时传入参数；不能为null</param>
        /// <param name="jsonIgnoreScriptTag">Json序列化时，是否忽略外层的Script标签</param>
        public ElasticBuildUpdateScriptModel(string id, object @params, bool jsonIgnoreScriptTag = false)
        {
            ThrowIfNullOrEmpty(Id = id);
            ThrowIfNull(Params = @params);
            JsonIgnoreScriptTag = jsonIgnoreScriptTag;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (JsonIgnoreScriptTag == true)
            {
                info.AddValue("id", Id);
                info.AddValue("params", Params);
            }
            else
            {
                info.AddValue("script", new
                {
                    id = Id,
                    @params = Params,
                });
            }
        }
        #endregion
    }
}
