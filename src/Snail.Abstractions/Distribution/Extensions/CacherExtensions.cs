using Snail.Abstractions.Identity.Interfaces;
using Snail.Utilities.Collections.Extensions;

namespace Snail.Abstractions.Distribution.Extensions
{
    /// <summary>
    /// <see cref="ICacher"/>扩展方法
    /// </summary>
    public static class CacherExtensions
    {
        #region 属性变量
        #endregion

        #region 公共方法

        #region 对象缓存
        /// <summary>
        /// 添加对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="key">对象Key</param>
        /// <param name="data">缓存对象</param>
        /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddObject<T>(this ICacher cacher, string key, T data, long? expireSeconds = null)
        {
            ThrowIfNullOrEmpty(key);
            ThrowIfNull(data);
            return cacher.AddObject(new Dictionary<string, T>() { { key, data } }, expireSeconds);
        }
        /// <summary>
        /// 添加对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="data">缓存对象；需实现<see cref="IIdentity"/>，从取主键Id值作为缓存Key</param>
        /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddObject<T>(this ICacher cacher, T data, long? expireSeconds = null) where T : IIdentity
            => AddObject<T>(cacher, [data], expireSeconds);
        /// <summary>
        /// 添加对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="datas">缓存对象；需实现<see cref="IIdentity"/>，从取主键Id值作为缓存Key</param>
        /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddObject<T>(this ICacher cacher, IList<T> datas, long? expireSeconds = null) where T : IIdentity
        {
            IDictionary<string, T> map = BuildCacheData(datas);
            return cacher.AddObject(map);
        }

        /// <summary>
        /// 获取对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="key">缓存key</param>
        /// <returns>存在返回数据，否则Default</returns>
        public static async Task<T?> GetObject<T>(this ICacher cacher, string key)
        {
            ThrowIfNullOrEmpty(key);
            IList<T>? lst = await cacher.GetObject<T>([key]);
            return lst.FirstOrDefault();
        }

        /// <summary>
        /// 移除对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="key">缓存key数组</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> RemoveObject<T>(this ICacher cacher, string key)
        {
            ThrowIfNullOrEmpty(key);
            return cacher.RemoveObject<T>([key]);
        }
        #endregion

        #region 哈希缓存
        /// <summary>
        /// 添加hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKey">缓存数据key</param>
        /// <param name="data">缓存数据</param>
        /// <param name="expireSeconds">整个hash的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddHash<T>(this ICacher cacher, string hashKey, string dataKey, T data, long? expireSeconds = null)
        {
            ThrowIfNullOrEmpty(hashKey);
            ThrowIfNullOrEmpty(dataKey);
            ThrowIfNull(data);
            return cacher.AddHash(hashKey, new Dictionary<string, T>() { { dataKey, data } }, expireSeconds);
        }
        /// <summary>
        /// 添加hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="data">缓存对象；需实现<see cref="IIdentity"/>，从取主键Id值作为缓存Key</param>
        /// <param name="expireSeconds">整个hash的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddHash<T>(this ICacher cacher, string hashKey, T data, long? expireSeconds = null) where T : IIdentity
            => AddHash<T>(cacher, hashKey, [data], expireSeconds);
        /// <summary>
        /// 添加hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="datas">缓存对象；需实现<see cref="IIdentity"/>，从取主键Id值作为缓存Key</param>
        /// <param name="expireSeconds">整个hash的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        public static Task<bool> AddHash<T>(this ICacher cacher, string hashKey, IList<T> datas, long? expireSeconds = null) where T : IIdentity
        {
            ThrowIfNullOrEmpty(hashKey);
            var map = BuildCacheData(datas);
            return cacher.AddHash(hashKey, map);
        }

        /// <summary>
        /// 获取hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKey">数据key值</param>
        /// <returns>缓存值，若不存在则返回Default</returns>
        public static async Task<T?> GetHash<T>(this ICacher cacher, string hashKey, string dataKey)
        {
            ThrowIfNullOrEmpty(dataKey);
            IList<T> list = await cacher.GetHash<T>(hashKey, [dataKey]);
            return list.FirstOrDefault();
        }

        /// <summary>
        /// 移除hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKey">缓存数据key</param>
        /// <returns>成功返回true，否则返回false</returns>
        public static Task<bool> RemoveHash<T>(this ICacher cacher, string hashKey, string dataKey)
        {
            ThrowIfNullOrEmpty(dataKey);
            return cacher.RemoveHash<T>(hashKey, [dataKey]);
        }
        #endregion

        #region SortedSet
        /// <summary>
        /// 添加SortedSet缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="cacher">缓存器</param>
        /// <param name="key">缓存key</param>
        /// <param name="data">缓存数据</param>
        /// <param name="score">数据分数，用于排序使用</param>
        /// <param name="expireSeconds">整个SortedSet的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true，否则返回false</returns>
        public static Task<bool> AddSortedSet<T>(this ICacher cacher, string key, T data, double score, long? expireSeconds = null) where T : notnull
        {
            ThrowIfNull(data);
            var datas = new Dictionary<T, double>() { { data, score } };
            return cacher.AddSortedSet(key, datas, expireSeconds);
        }
        #endregion

        #endregion

        #region 私有方法
        /// <summary>
        /// 构建缓存数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="datas"></param>
        /// <returns></returns>
        private static IDictionary<string, T> BuildCacheData<T>(IList<T> datas) where T : IIdentity
        {
            ThrowIfNullOrEmpty(datas);
            IDictionary<string, T> map = new Dictionary<string, T>();
            foreach (var data in datas)
            {
                ThrowIfNull(data, "data为null，无法基于IIdentity构建缓存数据");
                string key = ThrowIfNullOrEmpty(((IIdentity)data).Id);
                map[key] = data;
            }
            return map;
        }
        #endregion
    }
}
