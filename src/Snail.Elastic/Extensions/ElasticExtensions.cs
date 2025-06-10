using Newtonsoft.Json;
using Snail.Elastic.DataModels;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Elastic.Extensions
{
    /// <summary>
    /// Elastic相关扩展
    /// </summary>
    internal static class ElasticExtensions
    {
        #region 属性变量
        /// <summary>
        /// 忽略null值
        /// </summary>
        private static readonly JsonSerializerSettings _ignoreNullValue = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
        };
        #endregion

        #region 公共方法

        #region ElasticSearchModel
        /// <summary>
        /// 将ElasticSearch实体转成JSON字符串
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static string AsJson(this ElasticSearchModel search)
        {
            //  完全干掉null Key值
            return JsonConvert.SerializeObject(search, _ignoreNullValue);
        }
        #endregion

        #region ElasticSearchResult
        /// <summary>
        /// 查询结构的Source是有值：hits.hits是否有值
        /// </summary>
        /// <typeparam name="Source"></typeparam>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static bool IsSourceAny<Source>(this ElasticSearchResult<Source> ret) where Source : class
            => ret?.Hits?.Hits?.Count > 0;
        /// <summary>
        /// 提取查询结果的Source数据
        /// </summary>
        /// <typeparam name="Source"></typeparam>
        /// <param name="ret"></param>
        /// <returns></returns>
        public static IEnumerable<Source>? ToSource<Source>(this ElasticSearchResult<Source> ret) where Source : class
            => IsSourceAny(ret) ? ret.Hits!.Hits!.Select(hit => hit.Source!) : null;
        #endregion

        #region ElasticQueryModel 及 子类
        /// <summary>
        /// 合并查询条件
        /// </summary>
        /// <param name="queries">要合并的queries数据</param>
        /// <param name="matchType"></param>
        /// <returns>合并后的对象；传入<paramref name="queries"/>空，则返回null</returns>
        public static ElasticQueryModel? Combine(this List<ElasticQueryModel> queries, DbMatchType matchType)
            => Combine(queries?.ToArray(), matchType);
        /// <summary>
        /// 合并查询条件
        /// </summary>
        /// <param name="queries">要合并的queries数据</param>
        /// <param name="matchType"></param>
        /// <returns>合并后的对象；传入<paramref name="queries"/>空，则返回null</returns>
        public static ElasticQueryModel? Combine(this ElasticQueryModel[]? queries, DbMatchType matchType)
        {
            return queries?.Length switch
            {
                //  空直接返回null；若只有1个查询，则直接返回自身即可
                0 => null,
                1 => queries[0],
                //  组装bool查询：只支持and和or，其他的报错：ddz传了个1，不支持后面其他地方报错了，查起来太费劲了
                _ => matchType switch
                {
                    DbMatchType.AndAll => new ElasticBoolQueryModel() { Must = queries },
                    DbMatchType.OrAny => new ElasticBoolQueryModel() { Should = queries },
                    _ => throw new ArgumentException($"不支持的matchType：{matchType}"),
                }
            };
        }

        /// <summary>
        /// 针对当前查询条件取反
        /// </summary>
        /// <param name="query">要进行取反操作的查询条件</param>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>为null时</exception>
        /// <returns></returns>
        public static ElasticQueryModel Not(this ElasticQueryModel query)
        {
            //  取反后，将当前查询的Name值置空
            string? queryName = query.Name;
            query.Name = null;
            return new ElasticBoolQueryModel()
            {
                Name = queryName,
                MustNot = [query]
            };
        }

        /// <summary>
        /// 进行查询条件优化
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static ElasticQueryModel? Optimize(this ElasticQueryModel query)
        {
            /*  1、需要重点关注，若当前query有Name值，则不能随意往上抽取
             *  2、仅针对BoolQuery和NestedQuery做优化
             *      BoolQuery：
             *          1、若是Must+Should（只有1个+MinimumShould=1），则都合并到Must中
             *      NestedQuery：整理其Query，看看是否是BoolQuery
             */
            if (query == null)
            {
                return null;
            }
            query.Name = Default(query.Name, null);
            switch (query)
            {
                //  1、Nested字段处理：对 Query 查询条件做优化处理
                case ElasticNestedQueryModel nestedQuery:
                    nestedQuery.Query = Optimize(nestedQuery.Query)!;
                    return nestedQuery;
                //  2、Boolean查询处理：
                case ElasticBoolQueryModel boolQuery:
                    return Optimize(boolQuery);
                //  3、其他情况，直接返回自身
                default: return query;
            }
        }
        /// <summary>
        /// Bool查询条件优化：减少bool符合查询嵌套层级
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static ElasticQueryModel? Optimize(this ElasticBoolQueryModel? query)
        {
            //  强制默认值处理
            if (query == null)
            {
                return null;
            }
            query.Name = Default(query.Name, null);
            query.MinimumShould = Default(query.MinimumShould, null);
            //  遍历所有查询条件，做组合优化处理
            List<ElasticQueryModel>? must = null, should = null, filter = null, mustnot = null;
            bool bValue;
            //      must的处理：下属bool无Name值、且仅存在must或filter时，其must合并到query.must中，filter合并到query.filter中
            query.Must?.ForEach(itemQuery =>
            {
                itemQuery = Optimize(itemQuery)!;
                if (itemQuery == null)
                {
                    return;
                }
                if (itemQuery.Name == null && itemQuery is ElasticBoolQueryModel tmpQuery == true)
                {
                    if (tmpQuery.Should?.Any() != true && tmpQuery.MustNot?.Any() != true)
                    {
                        tmpQuery.Must?.AppendTo(ref must);
                        tmpQuery.Filter?.AppendTo(ref filter);
                        return;
                    }
                }
                //  兜底加入自身
                must ??= new List<ElasticQueryModel>();
                must.Add(itemQuery);
            });
            //      should的处理：下属bool无Name值、且仅有should时，其should展开到query.should中（仅在双方都未设置MinimumShould时，或=1时）
            query.Should?.ForEach(itemQuery =>
            {
                itemQuery = Optimize(itemQuery)!;
                if (itemQuery == null)
                {
                    return;
                }
                if (query.MinimumShould == null && itemQuery.Name == null && itemQuery is ElasticBoolQueryModel tmpQuery == true)
                {
                    bValue = (tmpQuery.MinimumShould == null || tmpQuery.MinimumShould == "1") && tmpQuery.Should?.Any() == true
                        && tmpQuery.Must?.Any() != true && tmpQuery.Filter?.Any() != true && tmpQuery.MustNot?.Any() != true;
                    if (bValue == true)
                    {
                        tmpQuery.Should?.AppendTo(ref should);
                        return;
                    }
                }
                //  兜底加入自身
                should ??= new List<ElasticQueryModel>();
                should.Add(itemQuery);
            });
            //      filter的处理：下属bool无Name值、且仅有must或filter时，其must和filter强制放到query.filter中（都不计算分数）
            query.Filter?.ForEach(itemQuery =>
            {
                itemQuery = Optimize(itemQuery)!;
                if (itemQuery == null)
                {
                    return;
                }
                if (itemQuery.Name == null && itemQuery is ElasticBoolQueryModel tmpQuery == true)
                {
                    if (tmpQuery.Should?.Any() != true && tmpQuery.MustNot?.Any() != true)
                    {
                        tmpQuery.Must?.AppendTo(ref filter);
                        tmpQuery.Filter?.AppendTo(ref filter);
                        return;
                    }
                }
                //  兜底加入自身
                filter ??= new List<ElasticQueryModel>();
                filter.Add(itemQuery);
            });
            //      mustnot处理：暂不做处理，对下属查询取反，情况比较复杂，暂时先忽略
            mustnot = query.MustNot?.Select(Optimize)?.Where(item => item != null)?.ToList()!;
            //  得到的数据做清理；针对must和should做一些特殊处理
            //      must有值，且should==1或者MinimumShould==should.length时；将should作为must条件值
            if (must?.Any() == true && should?.Any() == true)
            {
                bValue = (query.MinimumShould == null && should?.Count == 1) || (query.MinimumShould?.Equals(should?.Count) == true);
                if (bValue == true)
                {
                    must.AddRange(should!);
                    should = null;
                }
            }
            //      无mustnot且无filter时；若仅有must或者should，且只有1项时，强制返回选项自身：但需要注意Name值问题
            if (mustnot?.Any() != true && filter?.Any() != true)
            {
                bValue = (must?.Count == 1 && should?.Any() != true)
                    || (must?.Any() != true && should?.Count == 1 && (query.MinimumShould == null || query.MinimumShould == "1"));
                if (bValue == true)
                {
                    ElasticQueryModel tmpQuery = must?.FirstOrDefault() ?? should?.FirstOrDefault()!;
                    if (tmpQuery.Name == null || query.Name == null)
                    {
                        tmpQuery.Name ??= query.Name;
                        return tmpQuery;
                    }
                }
            }
            //  后续考虑针对集合中的nested字段再做优化：相同的path查询字段，合并到一起

            //  返回：合并得到的条件数据：空强制null，有条件的做去重（同一个对象实例仅存在一次就行了）
            query.Must = must?.Any() == true ? must.Distinct().ToArray() : null;
            query.Should = should?.Any() == true ? should.Distinct().ToArray() : null;
            query.Filter = filter?.Any() == true ? filter.Distinct().ToArray() : null;
            query.MustNot = mustnot?.Any() == true ? mustnot.Distinct().ToArray() : null;
            return query;
        }
        #endregion

        #region ElasticAggModel
        /// <summary>
        /// 添加子聚合
        /// </summary>
        /// <param name="agg"></param>
        /// <param name="childName"></param>
        /// <param name="childAgg"></param>
        /// <returns>返回<paramref name="agg"/>方便做链式调用</returns>
        public static ElasticAggModel AddChildAgg(this ElasticAggModel agg, string childName, ElasticAggModel childAgg)
        {
            ThrowIfNullOrEmpty(childName);
            ThrowIfNull(childAgg);
            //  
            agg.Aggs ??= new ElasticAggContainer();
            agg.Aggs[childName] = childAgg;
            return agg;
        }
        #endregion

        #endregion
    }
}
