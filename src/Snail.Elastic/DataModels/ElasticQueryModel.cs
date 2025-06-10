using System.Runtime.Serialization;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;

/*  将查询相关的实体合并放到此处统一管理  */
namespace Snail.Elastic.DataModels
{
    /// <summary>
    /// 查询条件基类
    /// </summary>
    public abstract class ElasticQueryModel
    {
        /// <summary>
        /// 查询名；不同查询类型下，构建JSON时自由定义
        /// </summary>
        public string? Name { set; get; }

        #region operator
        /// <summary>
        /// and拼接两查询条件
        /// </summary>
        /// <param name="query1"></param>
        /// <param name="query2"></param>
        /// <returns></returns>
        public static ElasticQueryModel? operator &(ElasticQueryModel? query1, ElasticQueryModel? query2)
        {
            return query1 == null || query2 == null
                ? query1 ?? query2
                : new ElasticBoolQueryModel()
                {
                    Must = [query1, query2]
                };

        }
        /// <summary>
        /// or拼接两查询条件
        /// </summary>
        /// <param name="query1"></param>
        /// <param name="query2"></param>
        /// <returns></returns>
        public static ElasticQueryModel? operator |(ElasticQueryModel? query1, ElasticQueryModel? query2)
        {
            return query1 == null || query2 == null
                ? query1 ?? query2
                : new ElasticBoolQueryModel()
                {
                    Should = [query1, query2]
                };
        }
        /// <summary>
        /// 查询条件取反 not
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static ElasticQueryModel? operator !(ElasticQueryModel query)
        {
            //  为null不处理；继承name值
            if (query == null)
            {
                return null;
            }
            string? name = query.Name;
            query.Name = null;
            return new ElasticBoolQueryModel()
            {
                Name = name,
                MustNot = [query]
            };
        }
        #endregion
    }
    /// <summary>
    /// 主键Ids查询
    /// </summary>
    [Serializable]
    public sealed class ElasticIdsQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的主键Id集合
        /// </summary>
        public string[] Ids { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="ids">要查询的主键Id集合</param>
        /// <exception cref="ArgumentNullException">ids为空</exception>
        /// <exception cref="ArgumentException">ids存在空字符串</exception>
        public ElasticIdsQueryModel(params string[] ids)
        {
            //  剔除重复数据；并判断是否有空数据
            var newIds = ids.Distinct();
            if (newIds.Any(string.IsNullOrEmpty) == true)
            {
                throw new ArgumentException("ids中存在空字符串数据");
            }
            ThrowIfNullOrEmpty(Ids = newIds.ToArray());
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
            Name = Default(Name, defaultStr: null);
            //  { "ids": { "values": ["111"],"_name":"idsq"} },
            info.AddValue("ids", new
            {
                _name = Name,
                values = Ids,
            });
        }
        #endregion
    }
    /// <summary>
    /// 字段存在性查询
    /// </summary>
    [Serializable]
    public sealed class ElasticExistsQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要判断存在性的字段
        /// </summary>
        public string Field { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要判断存在性的字段</param>
        public ElasticExistsQueryModel(string field)
        {
            ThrowIfNullOrEmpty(Field = field);
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
            Name = Default(Name, defaultStr: null);
            //{ "exists": { "field":"id","_name":"dddddd"} }
            info.AddValue("exists", new
            {
                _name = Name,
                field = Field
            });
        }
        #endregion
    }
    /// <summary>
    /// 布尔符合查询；把多个查询条件and、or条件组装起来
    /// </summary>
    [Serializable]
    public sealed class ElasticBoolQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 查询子句必须全部成立；对应SQL的AND查询
        /// </summary>
        public ElasticQueryModel[]? Must { set; get; }
        /// <summary>
        /// 查询子句都不成立；must的取反，但不影响计算score
        /// </summary>
        public ElasticQueryModel[]? MustNot { set; get; }

        /// <summary>
        /// 查询子句必须全部成立；对应SQL的AND查询<br />
        ///     和must一样，但不会影响计算score
        /// </summary>
        public ElasticQueryModel[]? Filter { set; get; }

        /// <summary>
        /// 查询子句只要有一个匹配；对应SQL的OR查询
        /// </summary>
        public ElasticQueryModel[]? Should { set; get; }
        /// <summary>
        /// Should 子句至少匹配几个。<br />
        ///     1、默认1；<br />
        ///     2、内部自动转数值；可给百分比；尽可能传数值，避免出现不可预知错误<br />
        ///     3、对应ES中“minimum_should_match”<br />
        /// </summary>
        public string? MinimumShould { set; get; }

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Name = Default(Name, null);
            //  不能都为空
            bool isValid = Must?.Any() == true || MustNot?.Any() == true || Filter?.Any() == true || Should?.Any() == true;
            ThrowIfFalse(isValid, "Must、MustNot、Filter、Should不能同时为空集合");
            /*
                "bool": {
                  "must": [
                    {"term": {"id": {"value": "11"}}}
                  ],
                  "_name":"xxxx"
                }
             */

            //  后续进行优化，若仅只有must，且只有一个查询条件，则整理自身

            info.AddValue("bool", new
            {
                _name = Name,
                must = Must?.Any() == true ? Must : null,
                must_not = MustNot?.Any() == true ? MustNot : null,
                filter = Filter?.Any() == true ? Filter : null,
                should = Should?.Any() == true ? Should : null,
                minimum_should_match = Default(MinimumShould, null)
            });
        }
        #endregion
    }
    /// <summary>
    /// 区间查询：between查询
    ///     支持数值、日期区间
    /// </summary>
    [Serializable]
    public sealed class ElasticRangeQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }

        /// <summary>
        /// 大于；若为数值和日期，es会自动转类型
        /// </summary>
        public string? GreaterThan { init; get; }
        /// <summary>
        /// 大于等于；若为数值和日期，es会自动转类型
        /// </summary>
        public string? GreaterEqual { init; get; }
        /// <summary>
        /// 小于；若为数值和日期，es会自动转类型
        /// </summary>
        public string? LessThan { init; get; }
        /// <summary>
        /// 小于等于；若为数值和日期，es会自动转类型
        /// </summary>
        public string? LessEqual { init; get; }

        /// <summary>
        /// 排序字段格式化<br />
        ///     在日期字段排序时可指定，其他情况无效果
        /// </summary>
        public string? Format { init; get; }
        /// <summary>
        /// 用于降低或提高 查询相关性分数的浮点数。默认为1.0.
        /// </summary>
        public float? Boost { init; get; }

        //  暂不提供：relation、time_zone配置

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <exception cref="ArgumentNullException">字段为空时</exception>
        public ElasticRangeQueryModel(string field)
        {
            ThrowIfNullOrEmpty(Field = field);
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
            Name = Default(Name, null);
            //  四个范围不能都为空
            Boolean isValid = GreaterThan?.Any() == true || GreaterEqual?.Any() == true
                           || LessThan?.Any() == true || LessEqual?.Any() == true;
            ThrowIfFalse(isValid, $"GreaterThan、GreaterEqual、LessThan、LessEqual不能同时为空");
            /*
                "range": {
                  "age": {
                    "gte": 10,
                    "lte": 20,
                    "boost": 1,
                    "_name":"xxx"
                  }
                }
             */
            info.AddValue("range", new Dictionary<String, Object>()
                .Set(Field, new
                {
                    _name = Name,
                    gt = GreaterThan,
                    gte = GreaterEqual,
                    lt = LessThan,
                    lte = LessEqual,
                    boost = Boost,
                    format = Format,
                })
            );
        }
        #endregion
    }
    /// <summary>
    /// 通配符查询，支持?、*通配符：类似like查询
    /// </summary>
    [Serializable]
    public sealed class ElasticWildcardQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 要查询的正则表达式语法
        /// </summary>
        public string Value { private init; get; }
        /// <summary>
        /// 是否忽略大小写；默认false<br />
        ///     对应es的case_insensitive
        /// </summary>
        public bool? IgnoreCase { private init; get; }

        /// <summary>
        /// 用于降低或提高 查询相关性分数的浮点数。默认为1.0.
        /// </summary>
        public float? Boost { init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="value">要查询的正则表达式语法</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <exception cref="ArgumentNullException">field或value为空时</exception>
        public ElasticWildcardQueryModel(string field, string value, bool? ignoreCase)
        {
            ThrowIfNullOrEmpty(Field = field);
            ThrowIfNullOrEmpty(Value = value);
            IgnoreCase = ignoreCase;
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
            Name = Default(Name, null);

            //"regexp": {"id":{"value": ".*2","_name":"xxx"}}
            info.AddValue("wildcard", new Dictionary<String, Object>()
                .Set(Field, new
                {
                    _name = Name,
                    value = Value,
                    case_insensitive = IgnoreCase,
                    boost = Boost,
                })
            );
        }
        #endregion

    }
    /// <summary>
    /// 正则表达式匹配查询
    /// </summary>
    [Serializable]
    public sealed class ElasticRegexpQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 要查询的正则表达式语法
        /// </summary>
        public string Value { private init; get; }
        /// <summary>
        /// 是否忽略大小写；默认false<br />
        ///     对应es的case_insensitive
        /// </summary>
        public bool? IgnoreCase { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="value">要查询的正则表达式语法</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <exception cref="ArgumentNullException">field或value为空时</exception>
        public ElasticRegexpQueryModel(String field, String value, Boolean? ignoreCase)
        {
            ThrowIfNullOrEmpty(Field = field);
            ThrowIfNullOrEmpty(Value = value);
            IgnoreCase = ignoreCase;
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
            Name = Default(Name, null);

            //"regexp": {"id":{"value": ".*2","_name":"xxx"}}
            info.AddValue("regexp", new Dictionary<String, Object>()
                .Set(Field, new
                {
                    _name = Name,
                    value = Value,
                    case_insensitive = IgnoreCase,
                })
            );
        }
        #endregion
    }
    /// <summary>
    /// 精确等值查询：=查询
    /// </summary>
    [Serializable]
    public sealed class ElasticTermQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 要查询值；若为数值和日期，es会自动转类型<br />
        ///     不能为空
        /// </summary>
        public string Value { private init; get; }

        /// <summary>
        /// 用于降低或提高 查询相关性分数的浮点数。默认为1.0.
        /// </summary>
        public float? Boost { init; get; }
        /// <summary>
        /// 是否忽略大小写；默认false<br />
        ///     对应es的case_insensitive
        /// </summary>
        public bool? IgnoreCase { init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="value">要查询值；若为数值和日期，es会自动转类型</param>
        /// <exception cref="ArgumentNullException">field或value为空时</exception>
        public ElasticTermQueryModel(string field, string value)
        {
            ThrowIfNullOrEmpty(Field = field);
            //  允许空字符串，但不允许null，elastic会报错的
            //ThrowIfNullOrEmpty(Value = value);
            ThrowIfNull(Value = value);
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="value">要查询值</param>
        /// <exception cref="ArgumentNullException">field或value为空时</exception>
        public ElasticTermQueryModel(string field, int value)
            : this(field, value.ToString())
        {
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
            Name = Default(Name, null);

            //"term": { "age":{ "value": "10","_name":"xxx"} }
            //  简单模式足足航：若仅仅只有Value有值，则进行简单组装
            if (Name == null && Boost == null && IgnoreCase == null)
            {
                info.AddValue("term", new Dictionary<String, String>()
                    .Set(Field, Value)
                );
            }
            else
            {
                //  全模式组装
                info.AddValue("term", new Dictionary<String, Object>()
                    .Set(Field, new
                    {
                        _name = Name,
                        value = Value,
                        boost = Boost,
                        case_insensitive = IgnoreCase,
                    })
                );
            }
        }
        #endregion
    }
    /// <summary>
    /// 元素包含查询：in查询
    ///     暂不支持lookup
    /// </summary>
    [Serializable]
    public sealed class ElasticTermsQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 要进行in查询匹配的元素<br />
        ///     若为数值和日期，es会自动转类型<br />
        ///     不能为空<br />
        /// </summary>
        public string[] Terms { private init; get; }

        /// <summary>
        /// 用于降低或提高 查询相关性分数的浮点数。默认为1.0.
        /// </summary>
        public float? Boost { init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="terms">要进行in查询匹配的元素</param>
        /// <exception cref="ArgumentNullException">field为空时</exception>
        /// <exception cref="ArgumentException">terms为空，或存在空字符串元素时</exception>
        public ElasticTermsQueryModel(string field, params string[] terms)
        {
            ThrowIfNullOrEmpty(Field = field);
            ThrowIfNull(Terms = terms);
            //  in 可查询空字符串，只是返回空而已；但不允许有null值
            ThrowIfTrue(terms.Any(item => item == null), "terms存在null值数据");
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">要查询的字段</param>
        /// <param name="terms">要进行in查询匹配的元素</param>
        public ElasticTermsQueryModel(string field, IEnumerable<string> terms) : this(field, terms.ToArray())
        { }
        #endregion

        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Name = Default(Name, null);

            /* "terms": {
             *  "age": ["10"],
             *  "_name":"xxx"
             * }
             */
            //  使用dict添加后，忽略null值的配置会失效，这里做一下特殊判断
            var dict = new Dictionary<string, object>();
            if (Name != null)
            {
                dict["_name"] = Name;
            }
            if (Field != null)
            {
                dict[Field] = Terms;
            }
            if (Boost != null)
            {
                dict["boost"] = Boost;
            }
            info.AddValue("terms", dict);
        }
        #endregion
    }
    /// <summary>
    /// 元素至少包含查询：in查询升级版
    /// </summary>
    [Serializable]
    public sealed class ElasticTermsSetQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的字段。不能为空
        /// </summary>
        public string Field { private init; get; }
        /// <summary>
        /// 要进行in查询匹配的元素<br />
        ///     若为数值和日期，es会自动转类型<br />
        ///     不能为空<br />
        /// </summary>
        public string[] Terms { private init; get; }

        /// <summary>
        /// 最小匹配数量字段<br />
        ///     1、当前索引中存在的字段名称，取其值作为最小匹配数量值<br />
        ///     2、对应es中“minimum_should_match_field”<br />
        /// </summary>
        /// <remarks>和<see cref="MinimumCount"/>二选一</remarks>
        public string? MinimumField { private init; get; }
        /// <summary>
        /// 最小匹配个数值<br />
        ///     1、最小匹配个数值；必须大于1；否则没有意义<br />
        ///     2、对应ES中“minimum_should_match_script”，自动生成固定值脚本；后续放开动态计算脚本<br />
        /// </summary>
        /// <remarks>和<see cref="MinimumField"/>二选一</remarks>
        public int? MinimumCount { private init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="terms">要进行in查询匹配的元素</param>
        /// <exception cref="ArgumentNullException">field、terms为空</exception>
        /// <exception cref="ArgumentException">terms存在空字符串数据</exception>
        private ElasticTermsSetQueryModel(string field, string[] terms)
        {
            ThrowIfNullOrEmpty(Field = field);
            ThrowIfNull(Terms = terms);
            ThrowIfNotTrue(Terms.Any(item => item == null), "terms存在null值数据");
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">字段</param>
        /// <param name="terms">要进行in查询匹配的元素</param>
        /// <param name="minimumField">最小匹配数量字段；当前索引中存在的字段名称，取其值作为最小匹配数量值</param>
        /// <exception cref="ArgumentNullException">minimumField为空</exception>
        public ElasticTermsSetQueryModel(string field, string[] terms, string minimumField) : this(field, terms)
        {
            MinimumField = ThrowIfNullOrEmpty(minimumField);
        }
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="field">字段名</param>
        /// <param name="terms">in匹配数据数组</param>
        /// <param name="minimumCount">最小匹配个数值；必须大于1；否则没有意义</param>
        /// <exception cref="ArgumentException">minimumCount不大于1时</exception>
        public ElasticTermsSetQueryModel(string field, string[] terms, int minimumCount) : this(field, terms)
        {
            ThrowIfFalse(minimumCount >= 1);
            MinimumCount = minimumCount;
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
            Name = Default(Name, defaultStr: null);
            ThrowIfTrue(MinimumField == null && MinimumCount.HasValue != true);

            //  针对MinimumField和MinimumCount做分支；这里全写上，JSON序列化时去掉null即可
            /*
             "terms_set":{
                  "names":{
                    "terms":["11","15"],
                    "minimum_should_match_script":{
                      "source":"1"
                    },
                    "_name":"xxx"
                  }
                }
             */
            info.AddValue("terms_set", new Dictionary<string, object>()
                .Set(Field, new
                {
                    _name = Name,
                    terms = Terms,
                    minimum_should_match_field = MinimumField,
                    minimum_should_match_script = MinimumCount.HasValue != true
                        ? null
                        : new
                        {
                            source = MinimumCount.ToString(),
                        }
                })
            );
        }
        #endregion
    }
    /// <summary>
    /// 嵌套nested查询
    /// </summary>
    [Serializable]
    public sealed class ElasticNestedQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 要查询的nested字段路径
        /// </summary>
        public string Path { private init; get; }
        /// <summary>
        /// nested字段中对象查询条件<br />
        ///     1、字段需要加上【<see cref="Path"/>.】
        /// </summary>
        public ElasticQueryModel Query { internal set; get; }

        /// <summary>
        /// nested匹配字段得分计算模式 <br />
        ///     1、取值范围： <br />
        ///         avg（默认）使用所有匹配子对象的平均相关性得分。 <br />
        ///         max 使用所有匹配子对象的最高相关性分数。 <br />
        ///         min 使用所有匹配子对象的最低相关性分数。 <br />
        ///         none 不要使用匹配子对象的相关性分数。该查询为父文档分配一个分数0。 <br />
        ///         sum 将所有匹配的子对象的相关性分数加在一起。 <br />
        ///     2、对应ES中“score_mode” <br />
        /// </summary>
        public string? ScoreMode { init; get; }
        /// <summary>
        /// 是否忽略Path为匹配的数据 <br />
        ///     1、false时，若path在文档中不存在，则会报错；true时，忽略错误 <br />
        ///     2、对应ES中“ignore_unmapped” <br />
        /// </summary>
        public bool? IgnoreUnmapped { init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="path">要查询的nested字段路径</param>
        /// <param name="query">nested字段中对象查询条件</param>
        /// <exception cref="ArgumentNullException">path或query为空时</exception>
        public ElasticNestedQueryModel(string path, ElasticQueryModel query)
        {
            ThrowIfNullOrEmpty(Path = path);
            ThrowIfNull(Query = query);
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
            Name = Default(Name, defaultStr: null);

            /*
                "nested": {
                    "path": "fieldvalues",
                    "query": {
                        "bool": {
                            "must_not": [
                                {"term": {"fieldvalues.key": {"value": "1660785427690340"}}}
                            ]
                        }
                    },
                    "_name":"xxxx"
                }
             */
            //  序列化时，若添加null值，不会被剔除掉；这里做一下特定处理
            info.AddValue("nested", new
            {
                _name = Name,
                path = Path,
                query = Query,
                score_mode = ScoreMode,
                ignore_unmapped = IgnoreUnmapped,
            });
        }
        #endregion
    }
    /// <summary>
    /// join字段的：has_child是否有子数据查询
    /// </summary>
    [Serializable]
    public sealed class ElasticHasChildQueryModel : ElasticQueryModel, ISerializable
    {
        /// <summary>
        /// 子数据关系类型：joins.name值
        /// </summary>
        public string Type { private init; get; }
        /// <summary>
        /// 子数据查询条件
        /// </summary>
        public ElasticQueryModel Query { private init; get; }

        /// <summary>
        /// nested匹配字段得分计算模式 <br />
        ///     1、取值范围： <br />
        ///         avg（默认）使用所有匹配子对象的平均相关性得分。 <br />
        ///         max 使用所有匹配子对象的最高相关性分数。 <br />
        ///         min 使用所有匹配子对象的最低相关性分数。 <br />
        ///         none 不要使用匹配子对象的相关性分数。该查询为父文档分配一个分数0。 <br />
        ///         sum 将所有匹配的子对象的相关性分数加在一起。 <br />
        ///     2、对应ES中“score_mode” <br />
        /// </summary>
        public string? ScoreMode { init; get; }
        /// <summary>
        /// 是否忽略Path为匹配的数据
        ///     1、false时，若path在文档中不存在，则会报错；true时，忽略错误
        ///     2、对应ES中“ignore_unmapped”
        /// </summary>
        public bool? IgnoreUnmapped { init; get; }

        /// <summary>
        /// 父包含的子文档个数最大值
        ///     1、超过此值父级文档则会从搜索结果中排除
        ///     2、对应ES的“max_children”
        /// </summary>
        public int? MaxChildren { init; get; }
        /// <summary>
        /// 父包含的子文档个数最小值
        ///     1、小于此值父级文档则会从搜索结果中排除
        ///     2、对应ES的“min_children”
        /// </summary>
        public int? MinChildren { init; get; }

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="type">子数据关系类型：joins.name值</param>
        /// <param name="query">子数据查询条件</param>
        public ElasticHasChildQueryModel(string type, ElasticQueryModel query)
        {
            ThrowIfNullOrEmpty(Type = type);
            ThrowIfNull(Query = query);
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
            Name = Default(Name, defaultStr: null);
            /*
                "has_child": {
                  "type": "group",
                  "query": {"match_all": {}},
                  "_name":"xxxxxxxx"
                }
             */
            info.AddValue("has_child", new
            {
                _name = Name,
                type = Type,
                query = Query,
                min_children = MinChildren,
                max_children = MaxChildren,
                score_mode = ScoreMode,
                ignore_unmapped = IgnoreUnmapped
            });
        }
        #endregion
    }
    /// <summary>
    /// 匹配所有查询：恒true查询条件
    /// </summary>
    [Serializable]
    public sealed class ElasticMatchAllQueryModel : ElasticQueryModel, ISerializable
    {
        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            /* {
                  "match_all":{"_name":"xxxx"}
                }, 
             */
            Name = Default(Name, null);
            Dictionary<String, String> dict = new();
            if (Name != null) dict["_name"] = Name;
            info.AddValue("match_all", dict);
        }
        #endregion
    }
    /// <summary>
    /// 不匹配任何数据查询<br />
    ///     1、恒false查询条件<br />
    ///     2、组装查询条件时，强制Name值为null<br />
    /// </summary>
    [Serializable]
    public sealed class ElasticMathNoneQueryModel : ElasticQueryModel, ISerializable
    {
        #region ISerializable
        /// <summary>
        /// JSON序列化
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            /* {
                  "match_none":{}
                }, 
             */
            //  恒false条件，强制Name值为空，没有意义
            info.AddValue("match_none", new { });
        }
        #endregion
    }
}
