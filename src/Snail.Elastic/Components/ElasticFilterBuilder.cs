using Newtonsoft.Json;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.Elastic.DataModels;
using Snail.Elastic.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Linq.Extensions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using static Snail.Elastic.Utils.ElasticBuilder;

namespace Snail.Elastic.Components
{
    /// <summary>
    /// Elastic过滤条件构建器
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class ElasticFilterBuilder<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 表达式类型不匹配的消息
        /// </summary>
        private static readonly string _typeNotMatchMessage = $"格式化后，表达式不为'Express<Fun<{typeof(DbModel).Name},Boolean>>'类型";
        /// <summary>
        /// 默认的筛选条件过滤器
        /// </summary>
        public static readonly ElasticFilterBuilder<DbModel> Default = new ElasticFilterBuilder<DbModel>(formatter: null);

        /// <summary>
        /// 过滤条件格式化器
        /// </summary>
        protected readonly DbFilterFormatter Formatter;
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public ElasticFilterBuilder(DbFilterFormatter? formatter)
        {
            Formatter = formatter ?? DbFilterFormatter.Default;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 构建过滤条件；不允许子类new覆盖
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        public ElasticQueryModel BuildFilter(IList<Expression<Func<DbModel, bool>>> filters)
        {
            //  禁止无条件构建
            ThrowIfNullOrEmpty(filters);
            ThrowIfHasNull(filters!);
            //  遍历构建数据，然后进行and合并
            List<ElasticQueryModel> queries = filters.Select(filter =>
            {
                //  格式化过滤表达式；验证格式化后的表达式有效性
                Expression? newFilter = Formatter.Visit(filter);
                if (newFilter == null)
                {
                    string msg = $"格式化后，过滤表达式为null。源表达式：{filter}";
                    throw new ApplicationException(msg);
                }
                if (newFilter is Expression<Func<DbModel, bool>> != true)
                {
                    string msg = $"{_typeNotMatchMessage}。源表达式：{filter}；格式化后：{newFilter}";
                    throw new ApplicationException(msg);
                }
                //  构建Elastic数据库过滤条件，并验证有效性；返回
                ElasticQueryModel query = StartBuildFilter((newFilter as LambdaExpression)!.Body);
                if (query == null)
                {
                    string msg = $"构建Mongo过滤条件为null。源表达式：{filter}；格式化后：{newFilter}";
                    throw new ApplicationException(msg);
                }
                return query;
            }).ToList();
            //  优化返回
            ElasticQueryModel query = queries.Combine(DbMatchType.AndAll)!.Optimize()!;
            return query;
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 开始构建过滤条件
        /// </summary>
        /// <param name="express"></param>
        /// <returns></returns>
        protected virtual ElasticQueryModel StartBuildFilter(Expression express)
        {
            //  基于节点类型做分发构建；
            ThrowIfNull(express);
            return express.NodeType switch
            {
                //  == != >= <= > <
                ExpressionType.Equal => BuildCompareFilter((express as BinaryExpression)!),
                ExpressionType.NotEqual => BuildCompareFilter((express as BinaryExpression)!),
                ExpressionType.GreaterThan => BuildCompareFilter((express as BinaryExpression)!),
                ExpressionType.GreaterThanOrEqual => BuildCompareFilter((express as BinaryExpression)!),
                ExpressionType.LessThan => BuildCompareFilter((express as BinaryExpression)!),
                ExpressionType.LessThanOrEqual => BuildCompareFilter((express as BinaryExpression)!),
                //  仅支持 && ||
                ExpressionType.AndAlso => BuildAndOrFilter((express as BinaryExpression)!, DbMatchType.AndAll),
                ExpressionType.OrElse => BuildAndOrFilter((express as BinaryExpression)!, DbMatchType.OrAny),
                //  不支持的情况说明格式化没处理好，直接报错
                _ => throw new NotSupportedException($"未支持的节点类型：{express.NodeType}；{express}"),
            };
        }

        /// <summary>
        /// 构建比较类型过滤条件：== != > >= &lt; &lt;=，或者"".contains，[].Contains
        /// </summary>
        /// <param name="binary"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected virtual ElasticQueryModel BuildCompareFilter(BinaryExpression binary)
        {
            //  左侧变量，右侧常量；注意左侧节点类型如果是Method类型，则需要做特定处理
            ThrowIfNull(binary);
            Expression left = binary.Left,
                       right = binary.Right;
            if (left.NodeType == ExpressionType.Constant)
            {
                throw new NotSupportedException($"左侧不能为常量类型：{binary}");
            }
            if (right.NodeType != ExpressionType.Constant)
            {
                throw new NotSupportedException($"右侧必须是常量类型：{binary}");
            }
            //  基于左侧类型做分发：先支持方法和DbModel成员属性
            MemberExpression? member;
            //      1、Method时，右侧必须得是Boolean常量；否则会出问题
            if (left.NodeType == ExpressionType.Call)
            {
                if (right.Type != typeof(bool))
                {
                    throw new NotSupportedException($"左侧为方法时，右侧必须是Boolean类型：{binary}");
                }
                //     1、分析是方法调用结果成立，还是不成立。==true !=true ==false !=false
                bool isTrue = right.GetConstValue<bool>() switch
                {
                    true => binary.NodeType == ExpressionType.Equal,
                    false => binary.NodeType == ExpressionType.NotEqual,
                };
                //      2、分析是成员调用，还是方法调用
                MethodCallExpression methodExpress = (left as MethodCallExpression)!;
                //          成员调用，先仅支持Text方法逻辑 regex查询逻辑
                if (DbFilterFormatter.TryAnalysisMemberExpress(methodExpress.Object, out member) == true)
                {
                    return BuildMemberLikeFilter(member!, methodExpress, isTrue);
                }
                //          非成员调用，先仅支持Contains方法逻辑 in查询逻辑
                if (methodExpress.Method.Name == "Contains")
                {
                    return BuildMemberInFilter(methodExpress, isTrue);
                }
            }
            //      2、成员变量，则构建== != >= <= > <
            if (DbFilterFormatter.TryAnalysisMemberExpress(left, out member) == true)
            {
                ThrowIfNull(member, $"member为空，无法构建比较查询条件。示例：item=>item.Name>'1'；{binary}");
                return BuildMemberCompareFilter(member!, binary.NodeType, (right as ConstantExpression)!);
            }
            //      3、最后兜底，走到这里不说明不支持了
            throw new NotSupportedException($"暂不支持的表达式，左侧无效：{binary}");
        }
        /// <summary>
        /// 构建And、Or过滤条件：&amp;&amp; ||
        /// </summary>
        /// <param name="binary"></param>
        /// <param name="matchType"></param>
        /// <returns></returns>
        protected virtual ElasticQueryModel BuildAndOrFilter(BinaryExpression binary, DbMatchType matchType)
        {
            //  左右两侧必须是比较的二元表达式；否则说明表达式的格式化有问题，提示出来
            ThrowIfNull(binary);
            //  下面的验证先去掉；后期看情况放开 有点忘了为啥要注释了，从【MongoDbFilterBuilder】抄过来的
            //BinaryExpression left = binary.Left as BinaryExpression,
            //    right = binary.Right as BinaryExpression;
            //if (left == null) throw new NotSupportedException($"左侧非二元表达式：{binary}");
            //if (right == null) throw new NotSupportedException($"右侧非二元表达式：{binary}");
            //  格式化左侧和右侧，最终拼接起来。只支持 &&、||
            List<ElasticQueryModel> query = new List<ElasticQueryModel> {
                StartBuildFilter(binary.Left),
                StartBuildFilter(binary.Right)
            };
            return query.Combine(matchType)!;
        }

        /// <summary>
        /// 构建成员节点的比较过滤条件
        ///     item.Name==null
        /// </summary>
        /// <param name="leftMember">左侧成员节点</param>
        /// <param name="compareType">比较类型 = != > >= &lt; &lt;= </param>
        /// <param name="rightConst">右侧常量节点 null 1、“1”</param>
        /// <returns></returns>
        protected virtual ElasticQueryModel BuildMemberCompareFilter(MemberExpression leftMember, ExpressionType compareType, ConstantExpression rightConst)
        {
            //  基于比较类型分发，确保各数据库相同条件返回值一致；屏蔽数据库差异；照着【SqlDbFilterBuilder】翻译，可能会有一些冗余，但代码可读性高
            /*      ElasticSearch中Null体现为 不存在*/
            string field = GetDbFieldName(leftMember);
            string? value = ConvertToString(rightConst.Value);
            switch (compareType)
            {
                //  等于：为null时，IS NULL；非null时，等值
                case ExpressionType.Equal:
                    return value == null
                        ? Exists(field, exists: false)
                        : Eq(field, value);
                //  不等于：为null时，IS NOT NULL；非null时，IS NULL OR 不等非null值
                case ExpressionType.NotEqual:
                    return value == null
                        ? Exists(field, exists: true)
                        : (Exists(field, exists: false) | Ne(field, value))!;
                //  大于：为null时，恒false；非null时，>
                case ExpressionType.GreaterThan:
                    return value == null
                        ? None()
                        : Gt(field, value);
                //  大于等于：为null时，恒false；非null时，>=
                case ExpressionType.GreaterThanOrEqual:
                    return value == null
                        ? None()
                        : Gte(field, value);
                //  小于：为null时，恒false；非null的值、且小于
                case ExpressionType.LessThan:
                    return value == null
                        ? None()
                        : (Exists(field, exists: true) & Lt(field, value))!;
                //  小于等于：为null时，恒false；非null的值、且小于等于
                case ExpressionType.LessThanOrEqual:
                    return value == null
                        ? None()
                        : (Exists(field, exists: true) & Lte(field, value))!;
                //  其他的不支持
                default:
                    string msg = $"不支持的比较类型：{compareType.ToString()}；属性名：{leftMember.Member.Name}";
                    throw new NotSupportedException(msg);
            }
        }
        /// <summary>
        /// 构建成员的like查询过滤条件
        ///     item.Name.StartsWith("")
        ///     item.Name.EndsWith("")
        ///     item.Name.Contains("")
        /// </summary>
        /// <param name="member">哪个成员调用的，如Item.Name</param>
        /// <param name="methodExpress">调用文本搜索的表达式，如item.Name.Contains</param>
        /// <param name="isTrue">方法调用是true还是false；如item.Contains("_")==true</param>
        /// <returns></returns>
        protected virtual ElasticQueryModel BuildMemberLikeFilter(MemberExpression member, MethodCallExpression methodExpress, bool isTrue)
        {
            /*  这里就不验证必须是字符串了和最小参数，若不是，则说明格式化没做好*/
            //  1、分析like值、是否忽略大小写、验证方法调用是否合法；分析出字段名
            string field = GetDbFieldName(member);
            string? value = DbFilterHelper.GetLikeQueryValue(methodExpress, out bool ignoreCase);
            //  2、进行分发调用
            switch (value)
            {
                //  like null，恒false；not like null 恒false
                case null:
                    return None();
                //  like ''，非null的所有值；not like ''，为null的值
                case "":
                    return isTrue
                        ? Exists(field, exists: true)
                        : Exists(field, exists: false);
                //  like有效值，正常like；not like有效值，null、或者不包含
                default:
                    switch (methodExpress.Method.Name)
                    {
                        case "Contains": value = string.Format("*{0}*", Regex.Escape(value)); break;
                        case "StartsWith": value = string.Format("{0}*", Regex.Escape(value)); break;
                        case "EndsWith": value = string.Format("*{0}", Regex.Escape(value)); break; ;
                        default: throw new NotSupportedException($"不支持的like方法{methodExpress.Method.Name}：{methodExpress}");
                    }
                    //  es 7.10.2 下wildcard类型字段使用Regexp做模糊查询，当查询一个字时（.*1.*)，会匹配出所有数据出来，改为使用wildcard查询
                    //  经过测试，这里正则关键字转义后，使用wildcard查询仍然可以（wildcard查询关键字只有 *和？）
                    return isTrue
                        ? Like(field, value, ignoreCase)
                        : (Exists(field, exists: false) | Nlike(field, value, ignoreCase))!;
            }
        }
        /// <summary>
        /// 构建成员的In过滤条件
        ///     new String[]{}.Contains(item.Name);
        /// </summary>
        /// <param name="methodExpress"></param>
        /// <param name="isTrue"></param>
        /// <returns></returns>
        protected virtual ElasticQueryModel BuildMemberInFilter(MethodCallExpression methodExpress, bool isTrue)
        {
            /*new List<String>().Contains(item.Name)   new String[].Contains(item.Name);*/
            //  分析出in查询的字段名称和in查询值
            object valuesT = DbFilterHelper.GetInQueryValues(methodExpress, out MemberExpression? member);
            List<string?>? values = valuesT == null
                ? null
                : JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(valuesT))
                    ?.Select(ConvertToString)
                    ?.ToList();
            string field = GetDbFieldName(member!);
            //  分发构建查询：根据null和values分发处理；查询条件可能会有一些冗余，但代码可读性高
            /*      利用 |、switch分发，减少if、else if、else if、else的使用
             *      0|0 =0(无null，无有效值)  1|0 =1(仅有null值)；0|10 =10(仅有效值) 1|10=11(有null，有in值)*/
            int tmpIndex = (values?.RemoveAll(value => value == null) > 0 ? 1 : 0) | (values?.Count > 0 ? 10 : 0);
            ElasticQueryModel? query;
            switch (tmpIndex)
            {
                //  in []，恒false条件；not in []，恒true条件
                case 0:
                    query = isTrue ? None() : All();
                    break;
                //  in [null]，=null；not in [null]，!=null
                case 1:
                    query = isTrue
                        ? Exists(field, exists: false)
                        : Exists(field, exists: true);
                    break;
                //      in [...有效值]，in有效值；not in [...有效值]，=null、或者not in 有效值
                case 10:
                    query = isTrue
                        ? In(field, values!)
                        : (Exists(field, exists: false) | Nin(field, values!))!;
                    break;
                //  in [null, ...有效值]，null、或者in有效值；not in [null, ...有效值]，!=null、且not in有效值
                case 11:
                    query = isTrue
                        ? Exists(field, exists: false) | In(field, values!)
                        : Exists(field, exists: true) & Nin(field, values!);
                    break;
                //  默认情况：不会进入，预防一下，做强制报错处理
                default:
                    throw new ApplicationException($"tmpIndex[{tmpIndex}]值异常；仅支持：0、1、10、11");
            }
            ;
            return query!;
        }

        /// <summary>
        /// 构建成员的数据字段名：为空报错
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        protected string GetDbFieldName(MemberExpression member)
        {
            if (ElasticModelRunner<DbModel>.FieldMap.TryGetValue(member.Member.Name, out var field) == false)
            {
                string msg = $"无法查找成员{member.Member.Name}对应的数据库字段名称";
                throw new KeyNotFoundException(msg);
            }
            return field.Name;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 将value值转换成es可识别的string值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string? ConvertToString(object? value)
        {
            /**
             * 特殊类型值做例外处理：
             *      1、bool值做特殊处理，否则toString后为“True”、“False”值，此时es会报错 
             *          Can't parse boolean value [True], expected [true] or [false]",
             *      2、datetime值强制格式；否则es会报错，这个和es中索引字段构建时指定格式有关，暂时强制
             *          failed to parse date field [2023/7/4 13:41:24] with format [strict_date_optional_time||epoch_millis]
             *          DateTime.AsJson之后，"\"2023-07-04T14:51:28.6277082+08:00\"" 实际期望的是"2023-07-04T14:51:28.6277082+08:00"
             *  考虑直接使用json序列化逻辑？将非string类型数据直接asjson处理？
             */
            if (value == null)
            {
                return null;
            }
            return value switch
            {
                string str => str,
                //  日期时间：查询时，外部可能传入的是2023/7/4 13:41:24，未带时区，此时需要做一下转换
                DateTime dt => dt.ToUniversalTime().AsJson().As<string>(),
                _ => value.AsJson(),
            };
        }
        #endregion
    }
}
