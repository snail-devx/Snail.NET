using System.Linq.Expressions;
using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;

namespace Snail.Abstractions.Database.Interfaces
{
    /// <summary>
    /// 数据库查询接口
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    /// <remarks>为了能使用from p in query 模式，这里接口尽可能和<see cref="IQueryable"/>靠拢</remarks>
    public interface IDbQueryable<DbModel> where DbModel : class
    {
        /// <summary>
        /// 查询条件<br />
        ///     1、多次调用时内部进行and合并<br />
        ///     2、Where条件使用备注：<br />
        ///         ⚠️ 不同数据库之间的 > >= = != &lt;&lt;=、in、like查询规则统一，但和数据库原生语法可能有差异；<br />
        ///         ⚠️ 若数据库自身默认忽略字段值大小写，则Where条件中指定无效，此差异无法屏蔽<br />
        ///         ⚠️ 约束：无效字段（值为null，或者字段不存在）；有效字段（值非null）<br/>
        ///     3、Where条件具体规则如下：<br />
        ///     ----- 【值比较】大于、小于、等于、大于等于、小于等于、不等于】-------------------<br/>
        ///         👉 ==  ：为null时，【无效字段】数据；非null时，【有效字段】正常比较<br/>
        ///         👉 !=  ：为null时，所有【有效字段】数据；非null时，【有效字段】且不等、或者【无效字段】<br/>
        ///         👉 >   ：为null时，恒false，不会命中任何数据；非null时，【有效字段】正常比较<br/>
        ///         👉 >=  ：为null时，恒false，不会命中任何数据；非null时，【有效字段】正常比较<br/>
        ///         👉 &lt; ：为null时，恒false，不会命中任何数据；非null时，【有效字段】且小于<br/>
        ///         👉 &lt;= ：为null时，恒false，不会命中任何数据；非null时，【有效字段】且小于等于<br/>
        ///     ----- 【in查询】new List{T}.Contains(item=>item.Name)、new String[]{}.Contains(item=>item.Name)  -------------------<br/>
        ///         👉 in []：恒false，不会命中任何数据<br/>
        ///         👉 not in []：恒true，命中所有数据<br/>
        ///         👉 in [null]：等效 =null<br/>
        ///         👉 not in [null]：等效 !=null<br/>
        ///         👉 in [null,...有效值]：=null 或者in有效值<br/>
        ///         👉 not in [in,...有效值]：!=null 且 not in有效值<br/>
        ///         👉 in [...有效值]：in有效值<br/>
        ///         👉 not in [...有效值]：=null 或 not in 有效值<br/>
        ///     ----- 【like查询】"".Contains(item=>item.Name);item=>item.Name.StartWith；item=>item.Name.EndWith-------------------<br/>
        ///         👉 like null ：恒false；不会命中任何数据<br/>
        ///         👉 not like null：恒false；不会命中任何数据<br/>
        ///         👉 like ''：非null的所有数据<br/>
        ///         👉 not like ''：为null的所有数据<br/>
        ///         👉 like '1'：包含1的所有数据<br/>
        ///         👉 not like '1'：为null、或者不包含1的所有数据<br/>
        /// </summary>
        /// <param name="predicate">where条件lambda表达式。lambda表达式目前不支持子文档、子表查询。示例:item=>item.Name=="Test"</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> Where(Expression<Func<DbModel, bool>> predicate);

        /// <summary>
        /// 数据升序<br />
        ///     1、多次调用按顺序合并
        /// </summary>
        /// <typeparam name="TField">排序字段类型</typeparam>
        /// <param name="fieldLambda">排序字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> OrderBy<TField>(Expression<Func<DbModel, TField>> fieldLambda);
        /// <summary>
        /// 数据降序<br />
        ///     1、多次调用按顺序合并
        /// </summary>
        /// <typeparam name="TField">排序字段类型</typeparam>
        /// <param name="fieldLambda">排序字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> OrderByDescending<TField>(Expression<Func<DbModel, TField>> fieldLambda);

        /// <summary>
        /// 查询时，返回的字段信息<br />
        ///     1、多次调用按顺序合并<br />
        ///     2、不调用此方法，则默认返回所有字段
        /// </summary>
        /// <typeparam name="TField">返回字段类型</typeparam>
        /// <param name="fieldLambda">返回字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
        /// <remarks>目前先支持属性字段，后续考虑select直接转成其他对象</remarks>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> Select<TField>(Expression<Func<DbModel, TField>> fieldLambda);
        /// <summary>
        /// 查询时，不返回的字段信息<br />
        ///     1、多次调用按顺序合并；<br />
        ///     2、不调用此方法，则默认返回所有字段<br />
        ///     3、若设置成了全部字段都不返回，则默认返回所有字段
        /// </summary>
        /// <typeparam name="TField"></typeparam>
        /// <param name="fieldLambda"></param>
        /// <returns></returns>
        IDbQueryable<DbModel> UnSelect<TField>(Expression<Func<DbModel, TField>> fieldLambda);

        /// <summary>
        /// 分页时忽略的数据量<br />
        ///     1、多次调用以最后一次调用为准<br />
        ///     2、当分页skip量大时，建议使用<see cref="Skip(string)"/>优化性能
        /// </summary>
        /// <param name="skip">分页忽略的数据量</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> Skip(int skip);
        /// <summary>
        /// 分页时，上一条数据的排序Key<br />
        ///     1、解决skip传入数值大时，查询性能慢的问题<br />
        ///     2、多次传入，以最后一次为准；skip数值互斥
        /// </summary>
        /// <param name="lastSortKey"></param>
        /// <returns></returns>
        /// <remarks>由上一次数据库查询生成；此字符串经过Base64编码。<see cref="DbQueryResult{DbModel}.LastSortKey"/></remarks>
        IDbQueryable<DbModel> Skip(string lastSortKey);
        /// <summary>
        /// 分页时取多少条数据<br />
        ///     1、多次调用以最后一次调用为准<br />
        /// </summary>
        /// <param name="count">当前页取的数据条数</param>
        /// <returns>数据库查询对象，方便链式调用</returns>
        IDbQueryable<DbModel> Take(int count);

        /// <summary>
        /// 符合Where条件的【所有数据】条数
        /// </summary>
        /// <remarks>仅使用Where条件做查询；Skip、Take、Order等失效</remarks>
        /// <returns>符合条件的数据条数</returns>
        Task<long> Count();
        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>存在返回true；否则返回false</returns>
        Task<bool> Any();

        /// <summary>
        /// 获取符合条件的第一条数据；
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据实体；无则返回默认值</returns>
        Task<DbModel?> FirstOrDefault();
        /// <summary>
        /// 获取符合筛选条件+分页的所有数据<br />
        ///     1、禁止无条件查询
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据库实体集合</returns>
        Task<IList<DbModel>> ToList();
        /// <summary>
        /// 获取符合筛选条件+分页的查询结果<br />
        ///     1、支持LastSortKey逻辑
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns></returns>
        Task<DbQueryResult<DbModel>> ToResult();


        //  后续考虑 Group Join 、、
    }
}