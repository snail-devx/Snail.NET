using Newtonsoft.Json;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.Mongo.Utils;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Linq.Extensions;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using static Snail.Mongo.Utils.MongoBuilder;

namespace Snail.Mongo.Components;

/// <summary>
/// Mongo过滤条件构建器
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public class MongoFilterBuilder<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 表达式类型不匹配的消息
    /// </summary>
    private static readonly string _typeNotMatchMessage = $"格式化后，表达式不为'Express<Fun<{typeof(DbModel).Name},Boolean>>'类型";
    /// <summary>
    /// 默认的过滤条件过滤器
    /// </summary>
    public static readonly MongoFilterBuilder<DbModel> Default = new MongoFilterBuilder<DbModel>();

    /// <summary>
    /// 过滤条件格式化器
    /// </summary>
    protected readonly DbFilterFormatter Formatter;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="formatter">过滤条件格式化器：为null使用默认的</param>
    public MongoFilterBuilder(DbFilterFormatter? formatter = null)
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
    public FilterDefinition<DbModel> BuildFilter(List<Expression<Func<DbModel, bool>>> filters)
    {
        ThrowIfNull(filters);
        ThrowIfHasNull(filters!);
        //  做技术研究，先用自带的，后期看性能，要是Mongo自带表达式解析快，再切回去
        List<FilterDefinition<DbModel>> dbFilters = filters.Select(filter =>
        {
            //  直接录用mongo自带的逻辑做功能
            //return new ExpressionFilterDefinition<DbModel>(filter);

            //  格式化过滤表达式；验证格式化后的表达式有效性
            Expression? newFilter = Formatter.Visit(filter);
            if (newFilter == null)
            {
                throw new ApplicationException($"格式化后，过滤表达式为null。源表达式：{filter}");
            }
            if (newFilter is Expression<Func<DbModel, bool>> != true)
            {
                string msg = $"{_typeNotMatchMessage}。源表达式：{filter}；格式化后：{newFilter}";
                throw new ApplicationException(msg);
            }
            //  构建Mongo数据库过滤条件，并验证有效性；返回
            FilterDefinition<DbModel> mongoFilter = StartBuildFilter((newFilter as LambdaExpression)!.Body);
            if (mongoFilter == null)
            {
                string msg = $"构建Mongo过滤条件为null。源表达式：{filter}；格式化后：{newFilter}";
                throw new ApplicationException();
            }
            return mongoFilter;
        }).ToList();
        //  组装返回结果：若存在多个，则用and拼接
        if (dbFilters.Any() == true)
        {
            FilterDefinition<DbModel> retFilter = dbFilters.Count == 1
                ? dbFilters.First()
                : Builders<DbModel>.Filter.And(dbFilters);
            return retFilter;
        }
        throw new ApplicationException($"构建Mongo过滤条件为空。{filters}");
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 开始构建过滤条件
    /// </summary>
    /// <param name="express"></param>
    /// <returns></returns>
    protected virtual FilterDefinition<DbModel> StartBuildFilter(Expression express)
    {
        //  基于节点类型做分发构建；
        ThrowIfNull(express);
        switch (express.NodeType)
        {
            //  == != >= <= > <
            case ExpressionType.Equal:
            case ExpressionType.NotEqual:
            case ExpressionType.GreaterThan:
            case ExpressionType.GreaterThanOrEqual:
            case ExpressionType.LessThan:
            case ExpressionType.LessThanOrEqual:
                return BuildCompareFilter((express as BinaryExpression)!);
            //  仅支持 && ||
            case ExpressionType.AndAlso:
                return BuildAndOrFilter((express as BinaryExpression)!, Builders<DbModel>.Filter.And);
            //  仅支持 && ||
            case ExpressionType.OrElse:
                return BuildAndOrFilter((express as BinaryExpression)!, Builders<DbModel>.Filter.Or);
            //  不支持的情况说明格式化没处理好，直接报错
            default: throw new NotSupportedException($"未支持的节点类型：{express.NodeType}；{express}");
        }
    }

    /// <summary>
    /// 构建比较类型过滤条件：== != &gt; &gt;= &lt; &lt;=，或者"".contains，[].Contains
    /// </summary>
    /// <param name="binary"></param>
    /// <returns></returns>
    protected virtual FilterDefinition<DbModel> BuildCompareFilter(BinaryExpression binary)
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
                string msg = $"左侧为方法时，右侧必须是Boolean类型：{binary}";
                throw new NotSupportedException();
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
                return BuildMemberLikeFilter(member!, methodExpress!, isTrue);
            }
            //          非成员调用，先仅支持Contains方法逻辑 in查询逻辑
            if (methodExpress.Method.Name == "Contains")
            {
                return BuildMemberInFilter(methodExpress, isTrue);
            }
            //          兜底，直接报错，不支持这些方法调用
            {
                string msg = $"Mongo过滤条件中暂不支持此方法调用表达式：{methodExpress}";
                throw new NotSupportedException(msg);
            }
        }
        //      2、成员变量
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
    /// <param name="linkFunc"></param>
    /// <returns></returns>
    protected virtual FilterDefinition<DbModel> BuildAndOrFilter(BinaryExpression binary, Func<FilterDefinition<DbModel>[], FilterDefinition<DbModel>> linkFunc)
    {
        //  左右两侧必须是比较的二元表达式；否则说明表达式的格式化有问题，提示出来
        ThrowIfNull(binary);
        ThrowIfNull(linkFunc);
        //  下面的验证先去掉；后期看情况放开
        //BinaryExpression left = binary.Left as BinaryExpression,
        //    right = binary.Right as BinaryExpression;
        //if (left == null) throw new NotSupportedException($"左侧非二元表达式：{binary}");
        //if (right == null) throw new NotSupportedException($"右侧非二元表达式：{binary}");
        //  格式化左侧和右侧，最终拼接起来。只支持 &&、||
        FilterDefinition<DbModel>
            leftFilter = StartBuildFilter(binary.Left),
            rightFilter = StartBuildFilter(binary.Right);
        return linkFunc([leftFilter, rightFilter]);
    }

    /// <summary>
    /// 构建成员节点的比较过滤条件
    ///     item.Name==null
    /// </summary>
    /// <param name="leftMember">左侧成员节点</param>
    /// <param name="compareType">比较类型 = != &gt; &gt;= &lt; &lt;= </param>
    /// <param name="rightConst">右侧常量节点 null 1、“1”</param>
    /// <returns></returns>
    protected virtual FilterDefinition<DbModel> BuildMemberCompareFilter(MemberExpression leftMember, ExpressionType compareType, ConstantExpression rightConst)
    {
        object? value = rightConst.Value;
        string field = GetDbFieldName(leftMember.Member.Name);
        //  基于比较类型分发，确保各数据库相同条件返回值一致；屏蔽数据库差异；照着【SqlDbFilterBuilder】翻译，可能会有一些冗余，但代码可读性高
        /*      ElasticSearch中Null体现为 ==null，undefined也会判定为==null*/
        switch (compareType)
        {
            //  等于：为null时，IS NULL；非null时，等值
            case ExpressionType.Equal:
                return value == null
                   ? Eq<DbModel>(field, null)
                   : Eq<DbModel>(field, value);
            //  不等于：为null时，IS NOT NULL；非null时，IS NULL OR 不等非null值
            case ExpressionType.NotEqual:
                return value == null
                   ? Ne<DbModel>(field, null)
                   : Eq<DbModel>(field, null) | Ne<DbModel>(field, value);
            //  大于：为null时，恒false（mysql默认，简化构建1<>1)；非null时，>
            case ExpressionType.GreaterThan:
                return value == null
                   ? None<DbModel>()
                   : Gt<DbModel>(field, value);
            //  大于等于：为null时，恒false（mysql默认，简化构建1<>1)；非null时，>=
            case ExpressionType.GreaterThanOrEqual:
                return value == null
                   ? None<DbModel>()
                   : Gte<DbModel>(field, value);
            //  小于：为null时，恒false（mysql默认，简化构建1<>1)；非null的值、且小于
            case ExpressionType.LessThan:
                return value == null
                   ? None<DbModel>()
                   : Ne<DbModel>(field, null) & Lt<DbModel>(field, value);
            //  小于等于：为null时，恒false（mysql默认，简化构建1<>1)；非null的值、且小于等于
            case ExpressionType.LessThanOrEqual:
                return value == null
                   ? None<DbModel>()
                   : Ne<DbModel>(field, null) & Lte<DbModel>(field, value);
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
    protected virtual FilterDefinition<DbModel> BuildMemberLikeFilter(MemberExpression member, MethodCallExpression methodExpress, Boolean isTrue)
    {
        /*  这里就不验证必须是字符串了和最小参数，若不是，则说明格式化没做好*/
        //  1、分析like值、是否忽略大小写、验证方法调用是否合法；分析出字段名
        string field = GetDbFieldName(member.Member.Name);
        string? value = DbFilterHelper.GetLikeQueryValue(methodExpress, out Boolean isIgnoreCase);
        //  2、进行分发调用
        switch (value)
        {
            //  like null，恒false；not like null 恒false
            case null:
                return None<DbModel>();
            //  like ''，非null的所有值；not like ''，为null的值
            case "":
                return isTrue
                    ? Ne<DbModel>(field, null)
                    : Eq<DbModel>(field, null);
            //  like有效值，正常like；not like有效值，null、或者不包含
            default:
                switch (methodExpress.Method.Name)
                {
                    case "Contains": value = string.Format(".*{0}.*", Regex.Escape(value)); break;
                    case "StartsWith": value = string.Format("^{0}.*", Regex.Escape(value)); break;
                    case "EndsWith": value = string.Format(".*{0}$", Regex.Escape(value)); break; ;
                    default: throw new NotSupportedException($"不支持的like方法{methodExpress.Method.Name}：{methodExpress}");
                }
                Regex regex = isIgnoreCase
                    ? new Regex(value, RegexOptions.IgnoreCase)
                    : new Regex(value);
                return isTrue
                    ? Like<DbModel>(field, regex)
                    : Eq<DbModel>(field, null) | Nlike<DbModel>(field, regex);
        }
    }
    /// <summary>
    /// 构建成员的In过滤条件
    ///     new String[]{}.Contains(item.Name);
    /// </summary>
    /// <param name="methodExpress"></param>
    /// <param name="isTrue"></param>
    /// <returns></returns>
    protected virtual FilterDefinition<DbModel> BuildMemberInFilter(MethodCallExpression methodExpress, bool isTrue)
    {
        /*new List<String>().Contains(item.Name)   new String[].Contains(item.Name);*/
        //  分析出in查询的属性名和in查询值
        object valuesT = DbFilterHelper.GetInQueryValues(methodExpress, out MemberExpression? member);
        List<object>? values = valuesT != null
            ? JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(valuesT))
            : null;
        string field = GetDbFieldName(member.Member.Name);
        //  分发构建查询：根据null和values分发处理；查询条件可能会有一些冗余，但代码可读性高
        /*      利用 |、switch分发，减少if、else if、else if、else的使用
         *      0|0 =0(无null，无有效值)  1|0 =1(仅有null值)；0|10 =10(仅有效值) 1|10=11(有null，有in值)*/
        int tmpIndex = (values?.RemoveAll(value => value == null) > 0 ? 1 : 0) | (values?.Count > 0 ? 10 : 0);
        FilterDefinition<DbModel> filter;
        switch (tmpIndex)
        {
            //  in []，恒false条件；not in []，恒true条件
            case 0:
                filter = isTrue ? None<DbModel>() : All<DbModel>();
                break;
            //  in [null]，=null；not in [null]，!=null
            case 1:
                filter = isTrue
                    ? Eq<DbModel>(field, null)
                    : Ne<DbModel>(field, null);
                break;
            //      in [...有效值]，in有效值；not in [...有效值]，=null、或者not in 有效值
            case 10:
                filter = isTrue
                    ? In<DbModel>(field, values!)
                    : Eq<DbModel>(field, null) | Nin<DbModel>(field, values!);
                break;
            //  in [null, ...有效值]，null、或者in有效值；not in [null, ...有效值]，!=null、且not in有效值
            case 11:
                filter = isTrue
                    ? Eq<DbModel>(field, null) | In<DbModel>(field, values!)
                    : Ne<DbModel>(field, null) & Nin<DbModel>(field, values!);
                break;
            //  默认情况：不会进入，预防一下，做强制报错处理
            default:
                throw new ApplicationException($"tmpIndex[{tmpIndex}]值异常；仅支持：0、1、10、11");
        }
        ;
        return filter;
    }

    /// <summary>
    /// 构建数据库字段名
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <returns></returns>
    protected string GetDbFieldName(string propertyName)
    {
        string? dbFieldName = MongoHelper.InferBsonMemberMap(typeof(DbModel), propertyName)?.ElementName;
        if (string.IsNullOrEmpty(dbFieldName) == true)
        {
            dbFieldName = $"无法查找成员{propertyName}对应的数据库字段名称";
            throw new KeyNotFoundException(dbFieldName);
        }
        return dbFieldName;
    }
    #endregion
}
