using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace Snail.Elastic.DataModels
{
    /// <summary>
    /// 集合操作容器
    /// </summary>
    [Serializable]
    public sealed class ElasticAggContainer : ISerializable
    {
        /* 为什么不直接使用Dictionary做操作？
         *  dictionary序列化时，外部传入全小写序列化配置后，会把聚合操作名称全小写；
         *  这里包裹封装后，进行自定义JSON序列化
         */

        /// <summary>
        /// 具体操作：key为聚合操作名；value为具体的聚合操作配置
        /// </summary>
        private readonly Dictionary<string, ElasticAggModel> _aggs = new();

        /// <summary>
        /// 获取或者设置指定的聚合操作配置
        ///     1、设置值时，若已存在相同name聚合操作，则覆盖
        ///     2、获取值时，若不存在，返回null
        ///     3、外部注意多线程影响
        /// </summary>
        /// <param name="aggName">聚合操作名称，不能为空，不能包含“[ ] >”</param>
        /// <returns></returns>
        public ElasticAggModel? this[string aggName]
        {
            set
            {
                ThrowIfNull(value);
                ThrowIfNullOrEmpty(aggName);
                ThrowIfTrue(aggName.Any(c => c == '[' || c == ']' || c == '>'), $"aggName不能有“[ ] >”字符：{aggName}");
                _aggs[aggName] = value!;
            }
            get
            {
                if (string.IsNullOrEmpty(aggName) == true)
                {
                    return null;
                }
                _aggs.TryGetValue(aggName, out ElasticAggModel? agg);
                return agg;
            }
        }

        #region 公共方法
        /// <summary>
        /// 是否有聚合数据
        /// </summary>
        /// <returns></returns>
        public bool Any() => _aggs.Count != 0;

        /// <summary>
        /// 合并传入容器的聚合操作到当前对象；若存在key相等的则覆盖掉
        /// </summary>
        /// <param name="container"></param>
        public void Combine(ElasticAggContainer container)
        {
            foreach (var kv in container._aggs)
            {
                _aggs[kv.Key] = kv.Value;
            }
        }
        /// <summary>
        /// 遍历聚合信息
        /// </summary>
        /// <param name="earch">若委托返回false，则跳出循环</param>
        public void ForEach(Func<ElasticAggModel, bool> earch)
        {
            ThrowIfNull(earch);
            foreach (var kv in _aggs)
            {
                bool bValue = earch(kv.Value);
                if (bValue == false)
                {
                    break;
                }
            }
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (_aggs.Count > 0)
            {
                foreach (var kv in _aggs)
                {
                    info.AddValue(kv.Key, kv.Value);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// 聚合操作基类
    /// </summary>
    public abstract class ElasticAggModel
    {
        /// <summary>
        /// 子的聚合操作配置
        /// </summary>
        public ElasticAggContainer? Aggs { set; get; }
    }

    /// <summary>
    /// Filter过滤聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticFilterAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// 过滤查询条件
        /// </summary>
        public ElasticQueryModel Filter { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="filter">过滤条件，不能为null</param>
        public ElasticFilterAggModel(ElasticQueryModel filter)
        {
            ThrowIfNull(filter);
            Filter = filter;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合信息
            info.AddValue("filter", Filter);
            //  构建子聚合
            if (Aggs?.Any() == true)
            {
                info.AddValue("aggs", Aggs);
            }
        }
        #endregion
    }
    /// <summary>
    /// 分组聚合；按照指定字段值做分组集合
    /// </summary>
    [Serializable]
    public sealed class ElasticTermsAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// 用于分组的字段
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 统计后要返回的统计结果数量（即分组数），默认10。最大值65536
        /// </summary>
        public int? Size { private init; get; }

        /// <summary>
        /// 统计结果是否升序
        ///     1、默认文档数量降序
        ///     2、“_count”=分组下文档数量,"_key"=分组字段值
        /// </summary>
        public bool? IsAsc { set; get; }
        /// <summary>
        /// 用于指定某些文档无统计字段时的默认值
        /// </summary>
        public string? Missing { set; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">用于分组的字段</param>
        /// <param name="size">统计后要返回的统计结果数量（即分组数），默认10。最大值65536</param>
        /// <param name="isAsc">统计结果是否升序;true升序，false降序</param>
        /// <param name="missing">用于指定某些文档无此字段时的默认值</param>
        public ElasticTermsAggModel(string field, int? size = null, bool? isAsc = null, string? missing = null)
        {
            ThrowIfNullOrEmpty(field);
            Field = field;
            Size = size;
            IsAsc = isAsc;
            Missing = missing;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合信息
            {
                JObject obj = new JObject();
                obj["field"] = Field;
                if (Size > 0)
                {
                    obj["size"] = Size.Value;
                }
                if (IsAsc.HasValue == true)
                {
                    JObject sort = new JObject();
                    sort["_key"] = IsAsc.Value == true ? "asc" : "desc";
                    obj["order"] = sort;
                }
                if (Missing != null)
                {
                    obj["missing"] = Missing;
                }
                info.AddValue("terms", obj);
            }
            //  构建子聚合
            if (Aggs?.Any() == true)
            {
                info.AddValue("aggs", Aggs);
            }
        }
        #endregion
    }
    /// <summary>
    /// 多字段分组聚合
    ///     1、按照指定的多个字段做排列
    /// </summary>
    [Serializable]
    public sealed class ElasticMultiTermsAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// 要进行分组的字段
        /// </summary>
        private readonly JArray _fields = new JArray();
        /// <summary>
        /// 统计后要返回的统计结果数量（即分组数），默认10。最大值65536
        /// </summary>
        private int? _Size;

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="size">统计后要返回的统计结果数量（即分组数），默认10。最大值65536</param>
        public ElasticMultiTermsAggModel(int? size = null)
        {
            _Size = size;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 添加聚合字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="missing">用于指定某些文档无此字段时的默认值</param>
        /// <returns>返回自身，做链式调用</returns>
        public ElasticMultiTermsAggModel AddField(string fieldName, string? missing = null)
        {
            ThrowIfNullOrEmpty(fieldName);
            JObject obj = new JObject();
            obj["field"] = fieldName;
            if (missing != null)
            {
                obj["missing"] = missing;
            }
            _fields.Add(obj);
            return this;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合
            ThrowIfTrue(_fields.Count == 0, "请先添加分组字段");
            info.AddValue("multi_terms", new JObject()
            {
                {"terms",_fields },
                { "size",_Size>0?_Size.Value:10}
            });
            //      size配置

            //  构建子聚合
            if (Aggs?.Any() == true) info.AddValue("aggs", Aggs);
        }
        #endregion
    }

    /// <summary>
    /// Nested聚合：将上下文跳转到指定Nested字段下做聚合操作
    /// </summary>
    [Serializable]
    public sealed class ElasticNestedAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// Nested字段路径
        /// </summary>
        public string Path { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="path">Nested字段路径；不能为空</param>
        public ElasticNestedAggModel(string path)
        {
            Path = ThrowIfNullOrEmpty(path);
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合信息
            info.AddValue("nested", new { path = Path });
            //  构建子聚合
            if (Aggs?.Any() == true)
            {
                info.AddValue("aggs", Aggs);
            }
        }
        #endregion
    }
    /// <summary>
    /// ReverseNested聚合：将上下文跳转到指定的Path路径下；可直接跳转到根文档
    /// </summary>
    [Serializable]
    public sealed class ElasticReverseNestedAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// 跳转的文档路径
        /// </summary>
        public string? Path { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="path">跳转的文档路径；为null表示跳转到根文档</param>
        public ElasticReverseNestedAggModel(string? path)
        {
            Path = path;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合信息：若Path为空，则直接到根文档
            info.AddValue("reverse_nested", Path?.Length > 0 ? new { path = Path } : new { });
            //  构建子聚合
            if (Aggs?.Any() == true)
            {
                info.AddValue("aggs", Aggs);
            }
        }
        #endregion
    }

    /// <summary>
    /// 数据统计相关聚合；由子类指定具体是sum、avg、、、
    /// </summary>
    [Serializable]
    public abstract class ElasticArithmeticAggModel : ElasticAggModel, ISerializable
    {
        /// <summary>
        /// 算数运算符：sum求和、avg求平均值、、
        /// </summary>
        public string Arithmetic { private init; get; }

        /// <summary>
        /// 数据字段
        /// </summary>
        public string Field { private init; get; }

        /// <summary>
        /// 用于指定某些文档无统计字段时的默认值
        /// </summary>
        public string? Missing { set; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="arithmetic">算数运算符：sum求和、avg求平均值、、</param>
        /// <param name="field">数据字段</param>
        /// <param name="missing">用于指定某些文档无统计字段时的默认值</param>
        protected ElasticArithmeticAggModel(string arithmetic, string field, string? missing = null)
        {
            Arithmetic = ThrowIfNullOrEmpty(arithmetic);
            Field = ThrowIfNullOrEmpty(field);
            Missing = missing;
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  自身聚合信息
            JObject obj = new JObject();
            obj["field"] = Field;
            if (Missing != null)
            {
                obj["missing"] = Missing;
            }
            info.AddValue(Arithmetic, obj);
            //  构建子聚合
            if (Aggs?.Any() == true)
            {
                info.AddValue("aggs", Aggs);
            }
        }
        #endregion
    }
    /// <summary>
    /// 数值字段求和聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticSumAggModel : ElasticArithmeticAggModel
    {
        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">数据字段；不可为空</param>
        /// <param name="missing">用于指定某些文档无统计字段时的默认值</param>
        public ElasticSumAggModel(string field, string? missing = null) : base("sum", field, missing)
        {
        }
        #endregion
    }
    /// <summary>
    /// 数值字段求平均值聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticAvgAggModel : ElasticArithmeticAggModel
    {
        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">数据字段；不可为空</param>
        /// <param name="missing">用于指定某些文档无统计字段时的默认值</param>
        public ElasticAvgAggModel(string field, string? missing = null) : base("avg", field, missing)
        {
        }
        #endregion
    }
    /// <summary>
    /// 数值字段最大值聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticMaxAggModel : ElasticArithmeticAggModel
    {
        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">数据字段；不可为空</param>
        /// <param name="missing">用于指定某些文档无统计字段时的默认值</param>
        public ElasticMaxAggModel(string field, string? missing = null) : base("max", field, missing)
        {
        }
        #endregion
    }
    /// <summary>
    /// 数值字段最小值聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticMinAggModel : ElasticArithmeticAggModel
    {
        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">数据字段；不可为空</param>
        /// <param name="missing">用于指定某些文档无统计字段时的默认值</param>
        public ElasticMinAggModel(string field, string? missing = null) : base("min", field, missing)
        {
        }
        #endregion
    }

    /// <summary>
    /// 数值的脚本聚合
    /// </summary>
    [Serializable]
    public sealed class ElasticMetricScriptAggModel : ElasticAggModel, ISerializable
    {
        #region 属性变量
        /// <summary>
        /// 全局的脚本参数
        /// </summary>
        public Dictionary<string, object>? Params { init; get; }

        /// <summary>
        /// init阶段脚本
        /// </summary>
        public required ScriptModel InitScript { init; get; }
        /// <summary>
        /// map阶段脚本
        /// </summary>
        public required ScriptModel MapScript { init; get; }
        /// <summary>
        /// combine阶段脚本
        /// </summary>
        public required ScriptModel CombineScript { init; get; }
        /// <summary>
        /// reduce阶段脚本
        /// </summary>
        public required ScriptModel ReduceScript { init; get; }
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认构造方法
        /// </summary>
        public ElasticMetricScriptAggModel()
        {
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="init">init阶段脚本</param>
        /// <param name="map">map阶段脚本</param>
        /// <param name="combine">combine阶段脚本</param>
        /// <param name="reduce">reduce阶段脚本</param>
        public ElasticMetricScriptAggModel(ScriptModel init, ScriptModel map, ScriptModel combine, ScriptModel reduce)
        {
            InitScript = ThrowIfNull(init);
            MapScript = ThrowIfNull(map);
            CombineScript = ThrowIfNull(combine);
            ReduceScript = ThrowIfNull(reduce);
        }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            //  偷个懒，空值直接给？？
            Dictionary<string, object> scripts = new()
        {
            {"params",Params??new ()},
            {
                "init_script",new Dictionary<string, object> ()
                {
                    {"id",InitScript.Id},
                    {"params",InitScript.Params??new ()},
                }
            },
            {
                "map_script",
                new Dictionary<string, object> ()
                {
                    {"id",MapScript.Id},
                    {"params",MapScript.Params??new ()},
                }
            },
            {
                "combine_script",
                new Dictionary<string, object> ()
                {
                    {"id",CombineScript.Id},
                    {"params",CombineScript.Params??new ()},
                }
            },
            {
                "reduce_script",
                new Dictionary<string, object> ()
                {
                    {"id",ReduceScript.Id},
                    {"params",ReduceScript.Params??new ()},
                }
            },
        };
            info.AddValue("scripted_metric", scripts);
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 脚本配置信息
        /// </summary>
        /// <param name="Id">脚本Id</param>
        /// <param name="Params">脚本参数，仅针对当前脚本生效</param>
        public record ScriptModel(string Id, Dictionary<string, object>? Params = null);
        #endregion
    }
}