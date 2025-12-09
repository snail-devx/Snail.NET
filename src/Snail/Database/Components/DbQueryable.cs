using Snail.Abstractions.Database.Attributes;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Interfaces;
using Snail.Database.Utils;
using Snail.Utilities.Linq.Extensions;
using System.Linq.Expressions;

namespace Snail.Database.Components;

/// <summary>
/// 数据库查询接口基类 <br />
///     1、将可查询接口中的查询条件部分，做集中实现
/// </summary>
/// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
public abstract class DbQueryable<DbModel> : IDbQueryable<DbModel> where DbModel : class
{
    #region 属性变量
    /// <summary>
    /// 调用【Select】方法写入的要返回字段信息集合
    /// </summary>
    private readonly List<string> _needs = new List<string>();
    /// <summary>
    /// 调用【UnSelect】方法写入的不需要返回的字段集合
    /// </summary>
    private readonly List<string> _unNeeds = new List<string>();

    /// <summary>
    /// 路由分片
    /// </summary>
    protected readonly string? Routing;
    /// <summary>
    /// 过滤条件
    /// </summary>
    protected readonly List<Expression<Func<DbModel, bool>>> Filters = new();
    /// <summary>
    /// 字段排序条件；key为字段名，value为排序规则，升序还是降序
    /// </summary>
    protected readonly List<KeyValuePair<string, bool>> Orders = new();
    /// <summary>
    /// 查询时，返回的字段信息；空表示返回所有字段
    /// </summary>
    protected readonly List<string> Selects = new List<string>();
    /// <summary>
    /// 分页时忽略的数据量
    /// </summary>
    protected int? Skip { private set; get; }
    /// <summary>
    /// 上一页最新的排序Key值；和skip互斥
    /// </summary>
    /// <remarks>由上一次数据库查询生成；此字符串经过Base64编码。<see cref="DbQueryResult{DbModel}.LastSortKey"/></remarks>
    protected string? LastSortKey { private set; get; }
    /// <summary>
    /// 分页时取多少条数据
    /// </summary>
    protected int? Take { private set; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    /// <param name="routing"></param>
    public DbQueryable(string? routing)
    {
        Routing = Default(routing, defaultStr: null);
    }
    #endregion

    #region IDbQueryable

    #region 当前类直接实现的
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
    IDbQueryable<DbModel> IDbQueryable<DbModel>.Where(Expression<Func<DbModel, bool>> predicate)
    {
        ThrowIfNull(predicate);
        Filters.Add(predicate);
        return this;
    }

    /// <summary>
    /// 数据升序
    ///     1、多次调用按顺序合并
    /// </summary>
    /// <typeparam name="TField">排序字段类型</typeparam>
    /// <param name="fieldLambda">排序字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
    /// <returns>数据库查询对象，方便链式调用</returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.OrderBy<TField>(Expression<Func<DbModel, TField>> fieldLambda)
    {
        string name = ThrowIfNull(fieldLambda).GetMember().Name;
        Orders.Add(new KeyValuePair<string, bool>(name, true));
        return this;
    }
    /// <summary>
    /// 数据降序
    ///     1、多次调用按顺序合并
    /// </summary>
    /// <typeparam name="TField">排序字段类型</typeparam>
    /// <param name="fieldLambda">排序字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
    /// <returns>数据库查询对象，方便链式调用</returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.OrderByDescending<TField>(Expression<Func<DbModel, TField>> fieldLambda)
    {
        string name = ThrowIfNull(fieldLambda).GetMember().Name;
        Orders.Add(new KeyValuePair<string, bool>(name, false));
        return this;
    }

    /// <summary>
    /// 查询时，返回的字段信息
    ///     1、多次调用按顺序合并
    ///     2、不调用此方法，则默认返回所有字段
    /// </summary>
    /// <typeparam name="TField">返回字段类型</typeparam>
    /// <param name="fieldLambda">返回字段lambda表达式。示例:item=>item.Name。不支持非成员字段</param>
    /// <remarks>目前先支持属性字段，后续考虑select直接转成其他对象</remarks>
    /// <returns>数据库查询对象，方便链式调用</returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.Select<TField>(Expression<Func<DbModel, TField>> fieldLambda)
    {
        //  要返回的字段，从【UnSelect】中移除
        string name = ThrowIfNull(fieldLambda).GetMember().Name;
        _unNeeds.Remove(name);
        _needs.Add(name);
        //  返回自身对象：返回前构建实际需要返回的字段值
        BuildSelects();
        return this;
    }
    /// <summary>
    /// 查询时，不返回的字段信息
    ///     1、多次调用按顺序合并
    ///     2、不调用此方法，则默认返回所有字段
    ///     3、若设置成了全部字段都不返回，则默认返回所有字段
    /// </summary>
    /// <typeparam name="TField"></typeparam>
    /// <param name="fieldLambda"></param>
    /// <returns></returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.UnSelect<TField>(Expression<Func<DbModel, TField>> fieldLambda)
    {
        //  不需要返回的字段，从【Select】中移除
        string name = ThrowIfNull(fieldLambda).GetMember().Name;
        _needs.Remove(name);
        _unNeeds.Add(name);
        //  返回自身对象：返回前构建实际需要返回的字段值
        BuildSelects();
        return this;
    }

    /// <summary>
    /// 分页时忽略的数据量
    ///     1、多次调用以最后一次调用为准
    /// </summary>
    /// <param name="skip">分页忽略的数据量</param>
    /// <returns>数据库查询对象，方便链式调用</returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.Skip(int skip)
    {
        //  传入值小于0无效null
        if (skip < 0)
        {
            string msg = $"Skip方法的skip参数不能小于0：{skip}";
            throw new ArgumentException(msg);
        }
        //  和LastSortKey互斥
        Skip = skip;
        LastSortKey = null;
        return this;
    }
    /// <summary>
    /// 分页时，上一条数据的排序Key
    ///     1、解决skip传入数值大时，查询性能慢的问题
    ///     2、多次传入，以最后一次为准；skip数值互斥
    /// </summary>
    /// <param name="lastSortKey"></param>
    /// <returns></returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.Skip(string lastSortKey)
    {
        //  和Skip数值互斥
        Skip = null;
        LastSortKey = Default(lastSortKey, defaultStr: null);
        return this;
    }
    /// <summary>
    /// 分页时取多少条数据
    ///     1、多次调用以最后一次调用为准
    /// </summary>
    /// <param name="count">当前页取的数据条数</param>
    /// <returns>数据库查询对象，方便链式调用</returns>
    IDbQueryable<DbModel> IDbQueryable<DbModel>.Take(int count)
    {
        if (count <= 0)
        {
            string msg = $"Take方法的count参数必须大于0：{count}";
            throw new ArgumentException(msg);
        }
        Take = count;
        return this;
    }
    #endregion

    #region 需要子类重写实现
    /// <summary>
    /// 符合Where条件的【所有数据】条数
    /// </summary>
    /// <remarks>仅使用Where条件做查询；Skip、Take、Order等失效</remarks>
    /// <returns>符合条件的数据条数</returns>
    public abstract Task<long> Count();
    /// <summary>
    /// 是否存在符合条件的数据
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>存在返回true；否则返回false</returns>
    public abstract Task<bool> Any();

    /// <summary>
    /// 获取符合条件的第一条数据；
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>数据实体；无则返回默认值</returns>
    public abstract Task<DbModel?> FirstOrDefault();
    /// <summary>
    /// 获取符合筛选条件+分页的所有数据<br />
    ///     1、禁止无条件查询
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns>数据库实体集合</returns>
    public abstract Task<IList<DbModel>> ToList();
    /// <summary>
    /// 获取符合筛选条件+分页的查询结果<br />
    ///     1、支持LastSortKey逻辑
    /// </summary>
    /// <remarks>Where、Order、Take、Skip都生效</remarks>
    /// <returns></returns>
    public abstract Task<DbQueryResult<DbModel>> ToResult();
    #endregion

    #endregion

    #region 继承方法
    /// <summary>
    /// 获取排序信息
    /// </summary>
    /// <param name="forceSortFieldName">强制排序的C#字段名；若传入了，但未使用，则基于【升序】补偿</param>
    /// <returns>kv集合，key为属性名称</returns>
    protected List<KeyValuePair<string, bool>> GetSorts(string? forceSortFieldName = null)
    {
        List<KeyValuePair<string, bool>> sorts = Orders.GroupBy(order => order.Key)
                .Select(group => new KeyValuePair<string, bool>(group.Key, group.Last().Value))
                .ToList();
        if (forceSortFieldName?.Length > 0)
        {
            bool bValue = sorts.Any(order => order.Key == forceSortFieldName);
            if (bValue == false)
            {
                sorts.Add(new(forceSortFieldName, true));
            }
        }
        return sorts;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建查询返回字段信息
    /// </summary>
    /// <returns></returns>
    private void BuildSelects()
    {
        //  先清空之前的选择数据
        if (Selects.Count > 0)
        {
            Selects.Clear();
        }
        //  包含和排除都为空时；使用默认的Null
        if (_needs.Count == 0 && _unNeeds.Count == 0)
        {
            return;
        }
        //  不包含字段为空时；使用包含字段值
        else if (_unNeeds.Count == 0)
        {
            Selects.TryAddRange(_needs);
        }
        //  有排除字段，则需要从选择字段从干掉（若_Needs无数据，则取所有字段）
        else
        {
            var needs = _needs.Count == 0
                ? DbModelHelper.GetTable<DbModel>().Fields.Select(field => field.Name)
                : _needs;
            needs.Except(_unNeeds).AppendTo(Selects);
        }
    }
    #endregion
}
