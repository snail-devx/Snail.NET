using Snail.Abstractions.Distribution;
using Snail.Abstractions.Distribution.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Distribution
{
    /// <summary>
    /// 分布式缓存器
    /// </summary>
    [Component<ICacher>(Lifetime = LifetimeType.Transient)]
    public sealed class Cacher : ICacher
    {
        #region 属性变量
        /// <summary>
        /// 缓存服务器
        /// </summary>
        private readonly IServerOptions _server;
        /// <summary>
        /// 缓存提供程序
        /// </summary>
        private readonly ICacheProvider _provider;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="server">缓存服务器配置选项</param>
        /// <param name="provider">缓存提供程序，用于实际读写缓存</param>
        public Cacher(IServerOptions server, ICacheProvider provider)
        {
            _server = ThrowIfNull(server);
            _provider = ThrowIfNull(provider);
        }
        #endregion

        #region ICacher：先仅对【ICacheProvider】进行中转调用，后期加入其他逻辑

        #region 对象缓存
        /// <summary>
        /// 添加对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="datas">缓存数据字典，Key为缓存key，Value为缓存数据</param>
        /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.AddObject<T>(IDictionary<string, T> datas, long? expireSeconds)
            => _provider.AddObject(datas, expireSeconds, _server);

        /// <summary>
        /// 判断对象缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> ICacher.HasObject<T>(string key)
            => _provider.HasObject<T>(key, _server);

        /// <summary>
        /// 获取对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="keys">缓存key数组</param>
        /// <returns>缓存数据集合</returns>
        Task<IList<T>> ICacher.GetObject<T>(IList<string> keys)
            => _provider.GetObject<T>(keys, _server);

        /// <summary>
        /// 移除对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="keys">缓存key数组</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveObject<T>(IList<string> keys)
            => _provider.RemoveObject<T>(keys, _server);
        #endregion

        #region 哈希缓存
        /// <summary>
        /// 添加hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="datas">缓存数据字典，Key为缓存key，Value为缓存数据</param>
        /// <param name="expireSeconds">整个hash的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.AddHash<T>(string hashKey, IDictionary<string, T> datas, long? expireSeconds)
            => _provider.AddHash(hashKey, datas, expireSeconds, _server);

        /// <summary>
        /// 判断hash缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> ICacher.HasHash<T>(string hashKey)
            => _provider.HasHash<T>(hashKey, _server);
        /// <summary>
        /// 判断hash缓存的指定数据是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKey">缓存数据key值</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> ICacher.HasHash<T>(string hashKey, string dataKey)
            => _provider.HasHash<T>(hashKey, dataKey, _server);

        /// <summary>
        /// 获取hash缓存的数据量
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>缓存数据个数</returns>
        Task<long> ICacher.GetHashLen<T>(string hashKey)
            => _provider.GetHashLen<T>(hashKey, _server);
        /// <summary>
        /// 获取hash缓存的所有数据key值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>数据key列表</returns>
        Task<IList<string>> ICacher.GetHashKeys<T>(string hashKey)
            => _provider.GetHashKeys<T>(hashKey, _server);
        /// <summary>
        /// 获取hash缓存的所有数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>缓存数据字典，Key为缓存key，Value为缓存数据</returns>
        Task<IDictionary<string, T>> ICacher.GetHash<T>(string hashKey)
            => _provider.GetHash<T>(hashKey, _server);
        /// <summary>
        /// 获取hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKeys">缓存数据key数组</param>
        /// <returns>缓存数据集合</returns>
        Task<IList<T>> ICacher.GetHash<T>(string hashKey, IList<string> dataKeys)
            => _provider.GetHash<T>(hashKey, dataKeys, _server);

        /// <summary>
        /// 移除hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveHash<T>(string hashKey)
            => _provider.RemoveHash<T>(hashKey, _server);
        /// <summary>
        /// 移除hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKeys">缓存数据key数组</param>
        /// <returns>成功返回true，否则返回false</returns>
        Task<bool> ICacher.RemoveHash<T>(string hashKey, IList<string> dataKeys)
            => _provider.RemoveHash<T>(hashKey, dataKeys, _server);
        #endregion

        #region SortedSet：有序列表缓存
        /// <summary>
        /// 添加SortedSet缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="datas">缓存数据字典，Key数据对象，Value为分数</param>
        /// <param name="expireSeconds">整个SortedSet的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true，否则返回false</returns>
        Task<bool> ICacher.AddSortedSet<T>(string key, IDictionary<T, double> datas, long? expireSeconds)
            => _provider.AddSortedSet(key, datas, expireSeconds, _server);

        /// <summary>
        /// 判断SortedSet缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> ICacher.HasSortedSet<T>(string key)
            => _provider.HasSortedSet<T>(key, _server);

        /// <summary>
        /// 获取SortedSet缓存的数据量
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>缓存数据个数</returns>
        Task<long> ICacher.GetSortedSetLen<T>(string key)
            => _provider.GetSortedSetLen<T>(key, _server);
        /// <summary>
        /// 获取SortedSet缓存的指定数据索引值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="data">数据对象</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>数据存在则返回具体索引位置；否则返回null</returns>
        Task<long?> ICacher.GetSortedSetRank<T>(string key, T data, bool ascending)
            => _provider.GetSortedSetRank(key, data, ascending, _server);
        /// <summary>
        /// 获取SortedSet缓存下指定索引范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据列表</returns>
        Task<IList<T>> ICacher.GetSortedSet<T>(string key, long startRank, long endRank, bool ascending)
            => _provider.GetSortedSet<T>(key, startRank, endRank, ascending, _server);
        /// <summary>
        /// 获取SortedSet缓存下指定索引范围数据，包含分数值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
        Task<IDictionary<T, double>> ICacher.GetSortedSetWithScore<T>(string key, long startRank, long endRank, bool ascending)
            => _provider.GetSortedSetWithScore<T>(key, startRank, endRank, ascending, _server);
        /// <summary>
        /// 获取SortedSet缓存下指定分数范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据列表</returns>
        Task<IList<T>> ICacher.GetSortedSet<T>(string key, double startScore, double endScore, bool ascending)
            => _provider.GetSortedSet<T>(key, startScore, endScore, ascending, _server);
        /// <summary>
        /// 获取SortedSet缓存下指定分数范围数据，包含分数值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
        Task<IDictionary<T, double>> ICacher.GetSortedSetWithScore<T>(string key, double startScore, double endScore, bool ascending)
            => _provider.GetSortedSetWithScore<T>(key, startScore, endScore, ascending, _server);

        /// <summary>
        /// 移除SortedSet缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveSortedSet<T>(string key)
            => _provider.RemoveSortedSet<T>(key, _server);
        /// <summary>
        /// 移除SortedSet缓存下指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="datas">缓存数据集合</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveSortedSet<T>(string key, IList<T> datas)
            => _provider.RemoveSortedSet(key, datas, _server);
        /// <summary>
        /// 移除SortedSet缓存下指定索引范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveSortedSet<T>(string key, long startRank, long endRank)
            => _provider.RemoveSortedSet<T>(key, startRank, endRank, _server);
        /// <summary>
        /// 移除SortedSet缓存下指定分数范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> ICacher.RemoveSortedSet<T>(string key, double startScore, double endScore)
            => _provider.RemoveSortedSet<T>(key, startScore, endScore, _server);
        #endregion

        #endregion
    }
}
