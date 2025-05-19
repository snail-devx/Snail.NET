using Snail.Abstractions.Database.DataModels;
using Snail.Database.Components;
using Snail.Database.Utils;

namespace Snail.Mongo
{
    /// <summary>
    /// <see cref="IDbQueryable{DbModel}"/>接口的Mongo实现
    /// </summary>
    /// <typeparam name="DbModel">数据库实体；需被<see cref="DbTableAttribute"/>特性标记</typeparam>
    public class MongoQueryable<DbModel> : DbQueryable<DbModel>, IDbQueryable<DbModel> where DbModel : class
    {
        #region 属性变量
        /// <summary>
        /// 数据库表对象
        /// </summary>
        protected readonly IMongoCollection<DbModel> DbCollection;
        /// <summary>
        /// 过滤条件构建器
        /// </summary>
        protected readonly MongoFilterBuilder<DbModel> FilterBuilder;
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        /// <param name="collection">数据表</param>
        /// <param name="filterBuilder">过滤条件构建器</param>
        /// <param name="routing">路由</param>
        public MongoQueryable(IMongoCollection<DbModel> collection, MongoFilterBuilder<DbModel>? filterBuilder, string? routing)
            : base(routing)
        {
            DbCollection = ThrowIfNull(collection);
            FilterBuilder = filterBuilder ?? MongoFilterBuilder<DbModel>.Default;
        }
        #endregion

        #region IDbQueryable：部分交给DbQueryable做默认实现
        /// <summary>
        /// 符合Where条件的【所有数据】条数
        /// </summary>
        /// <remarks>仅使用Where条件做查询；Skip、Take、Order等失效</remarks>
        /// <returns>符合条件的数据条数</returns>
        public override async Task<long> Count()
        {
            FilterDefinition<DbModel> filter = BuildFilter();
            long count = await DbCollection.Find(filter).CountDocumentsAsync();
            return count;
        }
        /// <summary>
        /// 是否存在符合条件的数据
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>存在返回true；否则返回false</returns>
        public override async Task<bool> Any()
        {
            //  mongo的any，内部用的FirstOrDefault逻辑，这里优化性能，强制只要id值返回
            var fluent = BuildFindFluent(false, out _).Project("{_id:\"1\"}").Limit(1);
            bool has = await fluent.AnyAsync();
            return has;
        }

        /// <summary>
        /// 获取符合条件的第一条数据；
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据实体；无则返回默认值</returns>
        public override async Task<DbModel?> FirstOrDefault()
        {
            var fluent = BuildFindFluent(true, out _);
            DbModel? model = await FirstOrDefault();
            return model;
        }
        /// <summary>
        /// 获取符合筛选条件+分页的所有数据<br />
        ///     1、禁止无条件查询
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns>数据库实体集合</returns>
        public override async Task<IList<DbModel>> ToList()
        {
            var fluent = BuildFindFluent(true, out _);
            IList<DbModel> list = await fluent.ToListAsync();
            return list ?? [];
        }
        /// <summary>
        /// 获取符合筛选条件+分页的查询结果<br />
        ///     1、支持LastSortKey逻辑
        /// </summary>
        /// <remarks>Where、Order、Take、Skip都生效</remarks>
        /// <returns></returns>
        async public override Task<DbQueryResult<DbModel>> ToResult()
        {
            IFindFluent<DbModel, DbModel> fluent = BuildFindFluent(true, out List<KeyValuePair<string, bool>> sorts, needSortField: true);
            IList<DbModel> list = await fluent.ToListAsync();
            //  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
            //return new DbQueryResult<DbModel>(list).BuildLastSortKey(sorts);
            DbQueryResult<DbModel> ret = new DbQueryResult<DbModel>(list?.ToArray());
            ret.LastSortKey = DbFilterHelper.GenerateLastSortKeyBySkipValue(Skip ?? 0, ret.Page ?? 0);
            return ret;
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 基于【Where】条件构建查询条件
        /// </summary>
        /// <returns></returns>
        protected FilterDefinition<DbModel> BuildFilter() => FilterBuilder.BuildFilter(Filters);

        /// <summary>
        /// 构建查询对象：find+order+limit+Project
        /// </summary>
        /// <param name="needProject">是否需要进行字段裁剪处理</param>
        /// <param name="sorts">out参数：把分析出来的排序情况返回给外部使用；String为字段名，value为升序/降序</param>
        /// <param name="needSortField">是否需要返回排序字段值，在ToResult时，需传true；否则会导致lastsortkey出问题</param>
        /// <returns></returns>
        protected IFindFluent<DbModel, DbModel> BuildFindFluent(bool needProject, out List<KeyValuePair<string, bool>> sorts, bool needSortField = false)
        {
            //  1、准备工作：梳理排序字段，方便后续LastSortKey和排序使用：强制补位加上主键id升序
            sorts = GetSorts(DbModelHelper.GetTable<DbModel>().PKField.Property.Name);
            //  2、基于筛选条件，构建IFindFluent<DbModel, DbModel>
            IFindFluent<DbModel, DbModel> fluent;
            {
                //  使用【BuildLastSortKeyFilter】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
                //FilterDefinition<DbModel> filter = LastSortKey?.Length > 0
                //    ? BuildFilter() & BuildLastSortKeyFilter(sorts)
                //    : BuildFilter();
                FilterDefinition<DbModel> filter = BuildFilter();
                fluent = DbCollection.Find(filter);
            }
            //  2、构建Project
            if (needProject == true && Selects.Any() == true)
            {
                //  取需要返回的字段名称集合： 若为LastSortKey模式，则需要把排序字段Key也强制加进去，否则取不到值
                List<string> fieldNames;
                if (needSortField == true)
                {
                    fieldNames = sorts.Select(kv => kv.Key).ToList();
                    fieldNames.AddRange(Selects);
                }
                else fieldNames = Selects;
                //  构建Project：对数据做一下去重处理
                List<ProjectionDefinition<DbModel>> projects = fieldNames
                    .Distinct()
                    .Select(fieldName => Builders<DbModel>.Projection.Include(fieldName))
                    .ToList();
                fluent = fluent.Project<DbModel>(Builders<DbModel>.Projection.Combine(projects));
            }
            //  3、构建排序：orders已经把主键Id强制加进去了
            {
                SortDefinitionBuilder<DbModel> sBuilder = Builders<DbModel>.Sort;
                List<SortDefinition<DbModel>> sds = sorts
                    .Select(order => order.Value ? sBuilder.Ascending(order.Key) : sBuilder.Descending(order.Key))
                    .ToList();
                fluent = fluent.Sort(sBuilder.Combine(sds));
            }
            //  4、构建分页：LastSortKey模式下，不要Skip
            {
                if (Take > 0) fluent = fluent.Limit(Take);
                //  使用【LastSortKey】会有问题，目前没想到好的解决方式；还是使用skip逻辑；
                //if (LastSortKey?.Any() != true && Skip > 0) fluent = fluent.Skip(Skip);
                if (LastSortKey?.Length > 0)
                {
                    int skip = DbFilterHelper.GetSkipValueFromLastSortKey(LastSortKey);
                    (this as IDbQueryable<DbModel>).Skip(skip);
                }
                if (Skip > 0)
                {
                    fluent = fluent.Skip(Skip);
                }
            }

            return fluent;
        }
        #endregion
    }
}
