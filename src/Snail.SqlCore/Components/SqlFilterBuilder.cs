using System.Linq.Expressions;
using Newtonsoft.Json;
using Snail.Database.Components;
using Snail.Database.Utils;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Linq.Extensions;

namespace Snail.SqlCore.Components
{
    /// <summary>
    /// 关系型数据库过滤条件构建器
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class SqlFilterBuilder<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 表达式类型不匹配的消息
        /// </summary>
        private static readonly string _typeNotMatchMessage = $"格式化后，表达式不为'Express<Fun<{typeof(DbModel).Name},Boolean>>'类型";
        /// <summary>
        /// 委托：基于属性名，获取数据库字段名称
        /// </summary>
        private readonly Func<string, string> DbFieldNameFunc;

        /// <summary>
        /// 过滤条件格式化器
        /// </summary>
        protected readonly DbFilterFormatter Formatter;
        /// <summary>
        /// 参数前缀
        /// </summary>
        protected readonly string ParameterToken;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="formatter">过滤表达式格式化器；为null使用默认的</param>
        /// <param name="dbFieldNameFunc">数据库字段名称处理委托；key为属性名，返回字段名</param>
        /// <param name="parameterToken">参数前缀</param>
        public SqlFilterBuilder(DbFilterFormatter? formatter, Func<string, string> dbFieldNameFunc, string parameterToken)
        {
            Formatter = formatter ?? DbFilterFormatter.Default;
            DbFieldNameFunc = ThrowIfNull(dbFieldNameFunc);
            ParameterToken = ThrowIfNull(parameterToken);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 构建过滤条件；不允许子类new覆盖
        /// </summary>
        /// <param name="filters">过滤条件表达式集合</param>
        /// <param name="param">过滤条件中的参数化对象，key为参数名称、value为参数值</param>
        /// <returns>不带Where的条件过滤sql语句</returns>
        public string BuildFilter(IList<Expression<Func<DbModel, bool>>> filters, out IDictionary<string, object> param)
        {
            ThrowIfNullOrEmpty(filters);
            ThrowIfHasNull(filters!);
            param = new Dictionary<string, object>();
            IList<string> whereSqls = new List<string>();
            //  遍历过滤条件，构建查询sql语句
            foreach (Expression<Func<DbModel, bool>> filter in filters)
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
                //  构建sql数据库过滤条件，并验证有效性
                string whereSql = StartBuildFilter((newFilter as LambdaExpression)!.Body, param);
                if (string.IsNullOrEmpty(whereSql) == true)
                {
                    throw new ApplicationException($"构建出来的sql语句为空：{newFilter}");
                }
                whereSqls.Add(whereSql);
            }
            //  组装sql并返回
            if (whereSqls.Count == 0)
            {
                throw new ApplicationException($"构建出来的sql语句为空");
            }
            return whereSqls.AsString(" AND ");
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 开始构建过滤条件
        /// </summary>
        /// <param name="express"></param>
        /// <param name="param">过滤条件中的参数化对象，确保一直往下传递，避免中途断掉；key为参数名称，value为参数值</param>
        /// <returns></returns>
        protected virtual string StartBuildFilter(Expression express, IDictionary<string, object> param)
        {
            //  基于节点类型做分发构建
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
                    return BuildCompareFilter((express as BinaryExpression)!, param);
                //  仅支持 && ||
                case ExpressionType.AndAlso:
                    return BuildAndOrFilter("AND", (express as BinaryExpression)!, param);
                case ExpressionType.OrElse:
                    return BuildAndOrFilter("OR", (express as BinaryExpression)!, param);
                //  不支持的情况说明格式化没处理好，直接报错
                default: throw new NotSupportedException($"未支持的节点类型：{express.NodeType}；{express}");
            }
        }

        /// <summary>
        /// 构建比较类型过滤条件：== != &gt; &gt;= &lt; &lt;=，或者"".contains，[].Contains
        /// </summary>
        /// <param name="binary"></param>
        /// <param name="param">过滤条件中的参数化对象，确保一直往下传递，避免中途断掉；key为参数名称，value为参数值</param>
        /// <returns></returns>
        protected virtual string BuildCompareFilter(BinaryExpression binary, IDictionary<string, object> param)
        {
            //  左侧变量，右侧常量；注意左侧节点类型如果是Method类型，则需要做特定处理
            ThrowIfNull(binary);
            Expression left = binary.Left, right = binary.Right;
            if (left.NodeType == ExpressionType.Constant) throw new NotSupportedException($"左侧不能为常量类型：{binary}");
            if (right.NodeType != ExpressionType.Constant) throw new NotSupportedException($"右侧必须是常量类型：{binary}");
            //  基于左侧类型做分发：先支持方法和DbModel成员属性
            MemberExpression? member;
            //      1、Method时，右侧必须得是Boolean常量；否则会出问题
            if (left.NodeType == ExpressionType.Call)
            {
                if (right.Type != typeof(bool)) throw new NotSupportedException($"左侧为方法时，右侧必须是Boolean类型：{binary}");
                //     1、分析是方法调用结果成立，还是不成立。==true !=true ==false !=false
                bool isTrue = right.GetConstValue<bool>() switch
                {
                    true => binary.NodeType == ExpressionType.Equal,
                    false => binary.NodeType == ExpressionType.NotEqual,
                };
                //      2、分析是成员调用，还是方法调用
                MethodCallExpression methodExpress = (left as MethodCallExpression)!;
                //          成员调用，先仅支持Text方法逻辑 like查询逻辑
                if (DbFilterFormatter.TryAnalysisMemberExpress(methodExpress.Object, out member) == true)
                {
                    return BuildMemberLikeFilter(member!, methodExpress, isTrue, param);
                }
                //          非成员调用，先仅支持Contains方法逻辑 in查询逻辑
                if (methodExpress.Method.Name == "Contains")
                {
                    return BuildMemberInFilter(methodExpress, isTrue, param);
                }
                //          兜底，直接报错，不支持这些方法调用
                throw new NotSupportedException($"SQL过滤条件中暂不支持此方法调用表达式：{methodExpress}");
            }
            //      2、成员变量之间的比较
            if (DbFilterFormatter.TryAnalysisMemberExpress(left, out member) == true)
            {
                ThrowIfNull(member, $"member为空，无法构建比较查询条件。示例：item=>item.Name>'1'；{binary}");
                return BuildMemberCompareFilter(member!, binary.NodeType, (right as ConstantExpression)!, param);
            }
            //      3、最后兜底，走到这里不说明不支持了
            throw new NotSupportedException($"暂不支持的表达式，左侧无效：{binary}");
        }
        /// <summary>
        /// 构建And、Or过滤条件：&amp;&amp; ||
        /// </summary>
        /// <param name="linkChar">链接符，and、or</param>
        /// <param name="binary"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string BuildAndOrFilter(string linkChar, BinaryExpression binary, IDictionary<string, object> param)
        {
            //  左右两侧必须是比较的二元表达式；否则说明表达式的格式化有问题，提示出来
            ThrowIfNull(binary);
            //  下面的验证先去掉；后期看情况放开
            //BinaryExpression left = binary.Left as BinaryExpression,
            //    right = binary.Right as BinaryExpression;
            //if (left == null) throw new NotSupportedException($"左侧非二元表达式：{binary}");
            //if (right == null) throw new NotSupportedException($"右侧非二元表达式：{binary}");
            //  格式化左侧和右侧，最终拼接起来。只支持 &&、||
            string leftFilter = StartBuildFilter(binary.Left, param),
                   rightFilter = StartBuildFilter(binary.Right, param);
            //  拼接前，做优先级限定 （）
            return $"({leftFilter} {linkChar} {rightFilter})";
        }

        /// <summary>
        /// 构建成员节点的比较过滤条件
        ///     item.Name==null
        /// </summary>
        /// <param name="leftMember">左侧成员节点</param>
        /// <param name="compareType">比较类型 = != &gt; &gt;= &lt; &lt;= </param>
        /// <param name="rightConst">右侧常量节点 null 1、“1”</param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string BuildMemberCompareFilter(MemberExpression leftMember, ExpressionType compareType, ConstantExpression rightConst, IDictionary<string, object> param)
        {
            object? value = rightConst.Value;
            string field = GetDbFieldName(leftMember.Member.Name);
            //  基于比较类型分发，确保各数据库相同条件返回值一致；屏蔽数据库差异
            switch (compareType)
            {
                //  等于：为null时，IS NULL；非null时，等值
                case ExpressionType.Equal:
                    return value == null
                        ? $"{field} IS NULL"
                        : $"{field} = {BuildSqlParameter(value, param)}";
                //  不等于：为null时，IS NOT NULL；非null时，IS NULL OR 不等非null值（mysql默认是【非null且不等于】）
                case ExpressionType.NotEqual:
                    return value == null
                        ? $"{field} IS NOT NULL"
                        : $"({field} IS NULL OR {field} <> {BuildSqlParameter(value, param)})";
                //  大于：为null时，恒false（mysql默认，简化构建1<>1)；非null时，>
                case ExpressionType.GreaterThan:
                    return value == null
                        ? $"1 <> 1"
                        : $"{field} > {BuildSqlParameter(value, param)}";
                //  大于等于：为null时，恒false（mysql默认，简化构建1<>1)；非null时，>=
                case ExpressionType.GreaterThanOrEqual:
                    return value == null
                        ? $"1 <> 1"
                        : $"{field} >= {BuildSqlParameter(value, param)}";
                //  小于：为null时，恒false（mysql默认，简化构建1<>1)；非null的值、且小于（mysql默认，简化表述，先加NOT NULL）
                case ExpressionType.LessThan:
                    return value == null
                        ? $"1 <> 1"
                        : $"({field} IS NOT NULL AND {field} < {BuildSqlParameter(value, param)})";
                //  小于等于：为null时，恒false（mysql默认，简化构建1<>1)；非null的值、且小于等于（mysql默认，简化表述，先加NOT NULL）
                case ExpressionType.LessThanOrEqual:
                    return value == null
                        ? $"1 <> 1"
                        : $"({field} IS NOT NULL AND {field} <= {BuildSqlParameter(value, param)})";
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
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string BuildMemberLikeFilter(MemberExpression member, MethodCallExpression methodExpress, bool isTrue, IDictionary<string, object> param)
        {
            /*  这里就不验证必须是字符串了和最小参数，若不是，则说明格式化没做好*/
            //  1、分析like值、是否忽略大小写、验证方法调用是否合法；分析出字段名
            string field = GetDbFieldName(member.Member.Name);
            string? value = DbFilterHelper.GetLikeQueryValue(methodExpress, out bool ignoreCase);
            //  2、进行分发调用
            switch (value)
            {
                //  like null，恒false；not like null 恒false
                case null:
                    return "1 <> 1";
                //  like ''，非null的所有值；not like ''，为null的值
                case "":
                    return isTrue
                        ? $"{field} IS NOT NULL"
                        : $"{field} IS NULL";
                //  like有效值，正常like；not like有效值，null、或者不包含
                default:
                    switch (methodExpress.Method.Name)
                    {
                        case "Contains": value = string.Format("%{0}%", value); break;
                        case "StartsWith": value = string.Format("{0}%", value); break;
                        case "EndsWith": value = string.Format("%{0}", value); break; ;
                        default: throw new NotSupportedException($"不支持的like方法{methodExpress.Method.Name}：{methodExpress}");
                    }
                    value = BuildTextLikeParamValue(value);
                    return isTrue
                        ? $"{field} LIKE {BuildSqlParameter(value, param)}"
                        : $"({field} IS NULL OR {field} NOT LIKE {BuildSqlParameter(value, param)})";
            }
        }
        /// <summary>
        /// 构建成员的In过滤条件
        ///     new String[]{}.Contains(item.Name);
        /// </summary>
        /// <param name="methodExpress"></param>
        /// <param name="isTrue"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected virtual string BuildMemberInFilter(MethodCallExpression methodExpress, bool isTrue, IDictionary<string, object> param)
        {
            /*new List<String>().Contains(item.Name)   new String[].Contains(item.Name);*/
            //  分析出in查询的字段名称和in查询值
            object valuesT = DbFilterHelper.GetInQueryValues(methodExpress, out MemberExpression member);
            List<object>? values = valuesT != null
                ? JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(valuesT))
                : null;
            string field = GetDbFieldName(member.Member.Name);
            //  分发构建查询：根据null和values分发处理；查询条件可能会有一些冗余，但代码可读性高
            /*      利用 |、switch分发，减少if、else if、else if、else的使用
             *      0|0 =0(无null，无有效值)  1|0 =1(仅有null值)；0|10 =10(仅有效值) 1|10=11(有null，有in值)*/
            int tmpIndex = (values?.RemoveAll(value => value == null) > 0 ? 1 : 0)
                | (values?.Count > 0 ? 10 : 0);
            switch (tmpIndex)
            {
                //  in []，恒false条件；not in []，恒true条件
                case 0:
                    return isTrue
                        ? "1 <> 1"
                        : "1 = 1";
                //  in [null]，=null；not in [null]，!=null
                case 1:
                    return isTrue
                        ? $"{field} IS NULL"
                        : $"{field} IS NOT NULL";
                //      in [...有效值]，in有效值；not in [...有效值]，=null、或者not in 有效值
                case 10:
                    return isTrue
                        ? $"{field} IN {BuildSqlParameter(values!, param)}"
                        : $"({field} IS NULL OR {field} NOT IN {BuildSqlParameter(values!, param)})";
                //  in [null, ...有效值]，null、或者in有效值；not in [null, ...有效值]，!=null、且not in有效值
                case 11:
                    return isTrue
                        ? $"({field} IS NULL OR {field} IN {BuildSqlParameter(values!, param)})"
                        : $"({field} IS NOT NULL AND {field} NOT IN {BuildSqlParameter(values!, param)})";
                //  默认情况：不会进入，预防一下，做强制报错处理
                default: throw new ApplicationException($"tmpIndex[{tmpIndex}]值异常；仅支持：0、1、10、11");
            };
        }

        /// <summary>
        /// 构建like参数值
        ///     1、主要解决like查询时，查询值包含特定关键字的情况
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual string BuildTextLikeParamValue(string value)
        {
            //  对关键字做转义处理 _ % \ ，先以mysql为准；有不满足的，后续可进行不同数据库之间的适配
            value = value
                .Replace(@"\", @"\\\")
                .Replace(@"_", @"\_")
                .Replace("%", @"\%");
            return value;
        }

        /// <summary>
        /// 构建成员的数据字段名：为空报错
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        protected string GetDbFieldName(string propertyName)
        {
            string dbFieldName = DbFieldNameFunc(propertyName);
            if (string.IsNullOrEmpty(dbFieldName) == true)
            {
                string msg = $"无法查找成员{propertyName}对应的数据库字段名称";
                throw new KeyNotFoundException(msg);
            }
            return dbFieldName;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 构建sql的参数
        /// </summary>
        /// <param name="paramValue">参数值</param>
        /// <param name="param">参数字典</param>
        /// <returns>参数化后的参数名称</returns>
        protected string BuildSqlParameter(object paramValue, IDictionary<string, object> param)
        {
            ThrowIfNull(param);
            ThrowIfNull(paramValue);
            //  基于参数格式做参数名称，避免出现参数名冲突的情况
            string paramName = $"Parameter{param.Count + 1}";
            param.Set(paramName, paramValue);
            return $"{ParameterToken}{paramName}";
        }
        #endregion
    }
}
