namespace Snail.Abstractions.Distribution
{
    /// <summary>
    /// 接口约束：分布式缓存器，用于实现对象、集合数据缓存
    /// </summary>
    /// <remarks>目前更多的是模仿Redis接口对外提供；先使用同步接口</remarks>
    public interface ICacher
    {
        #region 对象缓存
        /// <summary>
        /// 添加对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="datas">缓存数据字典，Key为缓存key，Value为缓存数据</param>
        /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> AddObject<T>(IDictionary<string, T> datas, long? expireSeconds = null);

        /// <summary>
        /// 判断对象缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> HasObject<T>(string key);

        /// <summary>
        /// 获取对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="keys">缓存key数组</param>
        /// <returns>缓存数据集合</returns>
        Task<IList<T>> GetObject<T>(IList<string> keys);

        /// <summary>
        /// 移除对象缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="keys">缓存key数组</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveObject<T>(IList<string> keys);
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
        Task<bool> AddHash<T>(string hashKey, IDictionary<string, T> datas, long? expireSeconds = null);

        /// <summary>
        /// 判断hash缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> HasHash<T>(string hashKey);
        /// <summary>
        /// 判断hash缓存的指定数据是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKey">缓存数据key值</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> HasHash<T>(string hashKey, string dataKey);

        /// <summary>
        /// 获取hash缓存的数据量
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>缓存数据个数</returns>
        Task<long> GetHashLen<T>(string hashKey);
        /// <summary>
        /// 获取hash缓存的所有数据key值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>数据key列表</returns>
        Task<IList<string>> GetHashKeys<T>(string hashKey);
        /// <summary>
        /// 获取hash缓存的所有数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>缓存数据字典，Key为缓存key，Value为缓存数据</returns>
        Task<IDictionary<string, T>> GetHash<T>(string hashKey);
        /// <summary>
        /// 获取hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKeys">缓存数据key数组</param>
        /// <returns>缓存数据集合</returns>
        Task<IList<T>> GetHash<T>(string hashKey, IList<string> dataKeys);

        /// <summary>
        /// 移除hash缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveHash<T>(string hashKey);
        /// <summary>
        /// 移除hash缓存的指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
        /// <param name="dataKeys">缓存数据key数组</param>
        /// <returns>成功返回true，否则返回false</returns>
        Task<bool> RemoveHash<T>(string hashKey, IList<string> dataKeys);
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
        Task<bool> AddSortedSet<T>(string key, IDictionary<T, double> datas, long? expireSeconds = null) where T : notnull;

        /// <summary>
        /// 判断SortedSet缓存是否存在
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>存在返回true，否则返回false</returns>
        Task<bool> HasSortedSet<T>(string key);

        /// <summary>
        /// 获取SortedSet缓存的数据量
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>缓存数据个数</returns>
        Task<long> GetSortedSetLen<T>(string key);
        /// <summary>
        /// 获取SortedSet缓存的指定数据索引值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="data">数据对象</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>数据存在则返回具体索引位置；否则返回null</returns>
        Task<long?> GetSortedSetRank<T>(string key, T data, bool ascending = true);
        /// <summary>
        /// 获取SortedSet缓存下指定索引范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据列表</returns>
        Task<IList<T>> GetSortedSet<T>(string key, long startRank, long endRank, bool ascending = true);
        /// <summary>
        /// 获取SortedSet缓存下指定索引范围数据，包含分数值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
        Task<IDictionary<T, double>> GetSortedSetWithScore<T>(string key, long startRank, long endRank, bool ascending = true) where T : notnull;
        /// <summary>
        /// 获取SortedSet缓存下指定分数范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据列表</returns>
        Task<IList<T>> GetSortedSet<T>(string key, double startScore, double endScore, bool ascending = true);
        /// <summary>
        /// 获取SortedSet缓存下指定分数范围数据，包含分数值
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <param name="ascending">升序还是降序，true升序，false降序</param>
        /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
        Task<IDictionary<T, double>> GetSortedSetWithScore<T>(string key, double startScore, double endScore, bool ascending = true) where T : notnull;

        /// <summary>
        /// 移除SortedSet缓存
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveSortedSet<T>(string key);
        /// <summary>
        /// 移除SortedSet缓存下指定数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="datas">缓存数据集合</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveSortedSet<T>(string key, IList<T> datas);
        /// <summary>
        /// 移除SortedSet缓存下指定索引范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startRank">起始位置</param>
        /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveSortedSet<T>(string key, long startRank, long endRank);
        /// <summary>
        /// 移除SortedSet缓存下指定分数范围数据
        /// </summary>
        /// <typeparam name="T">缓存数据类型</typeparam>
        /// <param name="key">缓存key</param>
        /// <param name="startScore">起始排序分数值</param>
        /// <param name="endScore">结束排序分数值</param>
        /// <returns>成功返回true；否则返回false</returns>
        Task<bool> RemoveSortedSet<T>(string key, double startScore, double endScore);
        #endregion
    }
}
