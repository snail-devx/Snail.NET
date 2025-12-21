using Snail.Abstractions.Distribution.Attributes;
using Snail.Abstractions.Distribution.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using Snail.Redis.Extensions;
using StackExchange.Redis;
using System.Data;

namespace Snail.Redis;

/// <summary>
/// Redis实现的缓存提供程序
/// </summary>
[Component<ICacheProvider>]
[Component<ICacheProvider>(Key = DIKEY_Redis)]
public class RedisCacheProvider : ICacheProvider
{
    #region 属性变量
    /// <summary>
    /// 默认过期时间：2小时，单位秒
    /// </summary>
    private const long DEFAULT_ExpireSeconds = 20 * 60 * 60;
    /// <summary>
    /// 缓存数据库索引，先默认0，后续支持外部做配置
    /// </summary>
    private const int DEFAULT_DbIndex = 0;
    /// <summary>
    /// redis管理器
    /// </summary>
    /// <remarks>通过依赖注入自动构建</remarks>
    private readonly RedisManager _manager;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="manager"></param>
    public RedisCacheProvider(RedisManager manager)
    {
        _manager = ThrowIfNull(manager);
    }
    #endregion

    #region ICacheProvider

    #region 对象缓存
    /// <summary>
    /// 添加对象缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="datas">缓存数据字典，Key为缓存key，Value为缓存数据</param>
    /// <param name="expireSeconds">过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    async Task<bool> ICacheProvider.AddObject<T>(IDictionary<string, T> datas, long? expireSeconds, IServerOptions server)
    {
        ThrowIfNullOrEmpty(datas);
        Type type = typeof(T);
        expireSeconds ??= DEFAULT_ExpireSeconds;
        //  处理缓存数据：设置缓存数据这里面可能有bug，多线程操作此字典后后出问题
        var caches = datas
            .Select(kv => new KeyValuePair<RedisKey, RedisValue>(BuildKey(type, kv.Key, cacheType: 0), kv.Value.AsValue()))
            .ToArray();
        IDatabase db = GetDatabase(server);
        bool bValue = await db.StringSetAsync(caches);
        await Parallel.ForEachAsync(caches, async (kv, _) => await KeyExpire(db, kv.Key, expireSeconds));
        return bValue;
    }
    /// <summary>
    /// 判断对象缓存是否存在
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ICacheProvider.HasObject<T>(string key, IServerOptions server)
        => KeyExists<T>(key, cacheType: 0, server);
    /// <summary>
    /// 获取对象缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="keys">缓存key数组</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据集合</returns>
    async Task<IList<T>> ICacheProvider.GetObject<T>(IList<string> keys, IServerOptions server)
    {
        ThrowIfNullOrEmpty(keys);
        Type type = typeof(T);
        RedisKey[] rKeys = keys.Select(key => BuildKey(type, key, cacheType: 0)).ToArray();
        RedisValue[] values = await GetDatabase(server).StringGetAsync(rKeys);
        return values.ToList<T>();
    }
    /// <summary>
    /// 移除对象缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="keys">缓存key数组</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    Task<bool> ICacheProvider.RemoveObject<T>(IList<string> keys, IServerOptions server)
        => KeyRemove<T>(keys, 0, server);
    #endregion

    #region 哈希缓存
    /// <summary>
    /// 添加hash缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="datas">缓存数据字典，Key为缓存key，Value为缓存数据</param>
    /// <param name="expireSeconds">整个hash的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    async Task<bool> ICacheProvider.AddHash<T>(string hashKey, IDictionary<string, T> datas, long? expireSeconds, IServerOptions server)
    {
        //hash暂时不支持设置key的过期时间；https://github.com/StackExchange/StackExchange.Redis/issues/801；先屏蔽掉；后期再支持
        //  基于ScriptEvaluate方法实现指定指令；

        ThrowIfNullOrEmpty(datas);
        expireSeconds ??= DEFAULT_ExpireSeconds;
        RedisKey key = BuildKey(typeof(T), hashKey, 1);
        HashEntry[] entrys = datas.Select(kv =>
        {
            ThrowIfNullOrEmpty(kv.Key, $"datas中存在为null或者空的key");
            return new HashEntry(new RedisValue(kv.Key), kv.Value.AsValue());
        }).ToArray();
        //  设置缓存
        IDatabase db = GetDatabase(server);
        await db.HashSetAsync(key, entrys);
        await KeyExpire(db, key, expireSeconds);

        return true;
    }
    /// <summary>
    /// 判断hash缓存是否存在
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ICacheProvider.HasHash<T>(string hashKey, IServerOptions server)
        => KeyExists<T>(hashKey, cacheType: 1, server);
    /// <summary>
    /// 判断hash缓存的指定数据是否存在
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="dataKey">缓存数据key值</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ICacheProvider.HasHash<T>(string hashKey, string dataKey, IServerOptions server)
    {
        ThrowIfNullOrEmpty(dataKey);
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        return GetDatabase(server).HashExistsAsync(key, new RedisValue(dataKey));
    }
    /// <summary>
    /// 获取hash缓存的数据量
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据个数</returns>
    Task<long> ICacheProvider.GetHashLen<T>(string hashKey, IServerOptions server)
    {
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        return GetDatabase(server).HashLengthAsync(key);
    }
    /// <summary>
    /// 获取hash缓存的所有数据key值
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>数据key列表</returns>
    async Task<IList<string>> ICacheProvider.GetHashKeys<T>(string hashKey, IServerOptions server)
    {
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        RedisValue[] values = await GetDatabase(server).HashKeysAsync(key);
        return values.Select(value => (string)value!).ToList();
    }
    /// <summary>
    /// 获取hash缓存的所有数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据字典，Key为缓存key，Value为缓存数据</returns>
    async Task<IDictionary<string, T>> ICacheProvider.GetHash<T>(string hashKey, IServerOptions server)
    {
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        HashEntry[] entries = await GetDatabase(server).HashGetAllAsync(key);
        return entries.ToDictionary<T>();
    }
    /// <summary>
    /// 获取hash缓存的指定数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="dataKeys">缓存数据key数组</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据集合</returns>
    async Task<IList<T>> ICacheProvider.GetHash<T>(string hashKey, IList<string> dataKeys, IServerOptions server)
    {
        ThrowIfNullOrEmpty(dataKeys);
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        RedisValue[] values = dataKeys.Select(key => new RedisValue(key)).ToArray();
        values = await GetDatabase(server).HashGetAsync(key, values);
        return values.ToList<T>();
    }
    /// <summary>
    /// 移除hash缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    Task<bool> ICacheProvider.RemoveHash<T>(string hashKey, IServerOptions server)
        => KeyRemove<T>([hashKey], cacheType: 1, server);
    /// <summary>
    /// 移除hash缓存的指定数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="hashKey">hash的key值；类似数据库的表名称</param>
    /// <param name="dataKeys">缓存数据key数组</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true，否则返回false</returns>
    async Task<bool> ICacheProvider.RemoveHash<T>(string hashKey, IList<string> dataKeys, IServerOptions server)
    {
        ThrowIfNullOrEmpty(dataKeys);
        RedisKey key = BuildKey(typeof(T), hashKey, cacheType: 1);
        RedisValue[] values = dataKeys.Select(key => new RedisValue(key)).ToArray();
        await GetDatabase(server).HashDeleteAsync(key, values);
        return true;
    }
    #endregion

    #region SortedSet：有序列表缓存
    /// <summary>
    /// 添加SortedSet缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="datas">缓存数据字典，Key数据对象，Value为分数</param>
    /// <param name="expireSeconds">整个SortedSet的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true，否则返回false</returns>
    async Task<bool> ICacheProvider.AddSortedSet<T>(string key, IDictionary<T, double> datas, long? expireSeconds, IServerOptions server)
    {
        ThrowIfNullOrEmpty(datas);
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        SortedSetEntry[] values = datas
            .Select(kv => new SortedSetEntry(element: kv.Key.AsValue(), score: kv.Value))
            .ToArray();
        //  设置缓存，并针对整体key设置过期时间
        IDatabase db = GetDatabase(server);
        await db.SortedSetAddAsync(setKey, values);
        await KeyExpire(db, setKey, expireSeconds);
        return true;
    }
    /// <summary>
    /// 判断SortedSet缓存是否存在
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>存在返回true，否则返回false</returns>
    Task<bool> ICacheProvider.HasSortedSet<T>(string key, IServerOptions server)
        => KeyExists<T>(key, cacheType: 2, server);
    /// <summary>
    /// 获取SortedSet缓存的数据量
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据个数</returns>
    Task<long> ICacheProvider.GetSortedSetLen<T>(string key, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        return GetDatabase(server).SortedSetLengthAsync(setKey);
    }
    /// <summary>
    /// 获取SortedSet缓存的指定数据索引值
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="data">数据对象</param>
    /// <param name="ascending">升序还是降序，true升序，false降序</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>数据存在则返回具体索引位置；否则返回null</returns>
    Task<long?> ICacheProvider.GetSortedSetRank<T>(string key, T data, bool ascending, IServerOptions server)
    {
        ThrowIfNull(data);
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        Order order = ascending == true ? Order.Ascending : Order.Descending;
        return GetDatabase(server).SortedSetRankAsync(setKey, data.AsValue(), order);
    }
    /// <summary>
    /// 获取SortedSet缓存下指定索引范围数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startRank">起始位置</param>
    /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
    /// <param name="ascending">升序还是降序，true升序，false降序</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据列表</returns>
    async Task<IList<T>> ICacheProvider.GetSortedSet<T>(string key, long startRank, long endRank, bool ascending, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        Order order = ascending == true ? Order.Ascending : Order.Descending;
        RedisValue[] values = await GetDatabase(server).SortedSetRangeByRankAsync(setKey, startRank, endRank, order);
        return values.ToList<T>();
    }
    /// <summary>
    /// 获取SortedSet缓存下指定索引范围数据，包含分数值
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startRank">起始位置</param>
    /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
    /// <param name="ascending">升序还是降序，true升序，false降序</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
    async Task<IDictionary<T, double>> ICacheProvider.GetSortedSetWithScore<T>(string key, long startRank, long endRank, bool ascending, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        Order order = ascending == true ? Order.Ascending : Order.Descending;
        SortedSetEntry[] values = await GetDatabase(server).SortedSetRangeByRankWithScoresAsync(setKey, startRank, endRank, order);
        return values.ToDictionary<T>();
    }
    /// <summary>
    /// 获取SortedSet缓存下指定分数范围数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startScore">起始排序分数值</param>
    /// <param name="endScore">结束排序分数值</param>
    /// <param name="ascending">升序还是降序，true升序，false降序</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据列表</returns>
    async Task<IList<T>> ICacheProvider.GetSortedSet<T>(string key, double startScore, double endScore, bool ascending, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        Order order = ascending == true ? Order.Ascending : Order.Descending;
        RedisValue[] values = await GetDatabase(server).SortedSetRangeByScoreAsync(setKey, startScore, endScore, Exclude.None, order);
        return values.ToList<T>();
    }
    /// <summary>
    /// 获取SortedSet缓存下指定分数范围数据，包含分数值
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startScore">起始排序分数值</param>
    /// <param name="endScore">结束排序分数值</param>
    /// <param name="ascending">升序还是降序，true升序，false降序</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>缓存数据字典，key为缓存数据，value为分数值</returns>
    async Task<IDictionary<T, double>> ICacheProvider.GetSortedSetWithScore<T>(string key, double startScore, double endScore, bool ascending, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        Order order = ascending == true ? Order.Ascending : Order.Descending;
        SortedSetEntry[] values = await GetDatabase(server).SortedSetRangeByScoreWithScoresAsync(setKey, startScore, endScore, Exclude.None, order);
        return values.ToDictionary<T>();
    }
    /// <summary>
    /// 移除SortedSet缓存
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    Task<bool> ICacheProvider.RemoveSortedSet<T>(string key, IServerOptions server)
        => KeyRemove<T>([key], cacheType: 2, server);
    /// <summary>
    /// 移除SortedSet缓存下指定数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="datas">缓存数据集合</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    async Task<bool> ICacheProvider.RemoveSortedSet<T>(string key, IList<T> datas, IServerOptions server)
    {
        ThrowIfNullOrEmpty(datas);
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        RedisValue[] values = datas.Select(data => data.AsValue()).ToArray();
        await GetDatabase(server).SortedSetRemoveAsync(setKey, values);
        return true;
    }
    /// <summary>
    /// 移除SortedSet缓存下指定索引范围数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startRank">起始位置</param>
    /// <param name="endRank">结束位置；传-1表示到整个set结尾</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    async Task<bool> ICacheProvider.RemoveSortedSet<T>(string key, long startRank, long endRank, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        await GetDatabase(server).SortedSetRemoveRangeByRankAsync(setKey, startRank, endRank);
        return true;
    }
    /// <summary>
    /// 移除SortedSet缓存下指定分数范围数据
    /// </summary>
    /// <typeparam name="T">缓存数据类型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="startScore">起始排序分数值</param>
    /// <param name="endScore">结束排序分数值</param>
    /// <param name="server">缓存服务器配置选项</param>
    /// <returns>成功返回true；否则返回false</returns>
    async Task<bool> ICacheProvider.RemoveSortedSet<T>(string key, double startScore, double endScore, IServerOptions server)
    {
        RedisKey setKey = BuildKey(typeof(T), key, cacheType: 2);
        await GetDatabase(server).SortedSetRemoveRangeByScoreAsync(setKey, startScore, endScore);
        return true;
    }
    #endregion

    #endregion

    #region 私有方法
    /// <summary>
    /// 构建Redis缓存Key
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="cacheType">Object：0，Hash：1；SortedSet：2。兼容net46下的操作，不然可以不用</param>
    /// <returns></returns>
    private static RedisKey BuildKey(Type type, string key, int cacheType)
    {
        ThrowIfNullOrEmpty(key);
        //  属性名做自定义支撑
        string typeName;
        {
            type = type.GetCustomAttribute<CacheAttribute>()?.Type ?? type;
            typeName = type.Name;
        }
        //  后期可考虑基于key拆分不同服务器存储
        return $"{typeName}:{cacheType}:{key}";
    }

    /// <summary>
    /// 获取缓存数据库
    /// </summary>
    /// <param name="server"></param>
    /// <returns></returns>
    private IDatabase GetDatabase(IServerOptions server)
        => _manager.GetDatabase(server, DEFAULT_DbIndex);

    /// <summary>
    /// 设置rediskey的过期时间
    /// </summary>
    /// <param name="db"></param>
    /// <param name="key"></param>
    /// <param name="expireSeconds">整个SortedSet的过期时间（单位秒）；为null则默认2小时，&lt;=0则始终不过期</param>
    /// <returns></returns>
    private static async Task<bool> KeyExpire(IDatabase db, RedisKey key, long? expireSeconds)
    {
        //  加多线程并发，实现快速设置过期时间；db自身没找到批量设置过期时间接口
        expireSeconds ??= DEFAULT_ExpireSeconds;
        if (expireSeconds > 0)
        {
            TimeSpan span = TimeSpan.FromSeconds(expireSeconds.Value);
            await db.KeyExpireAsync(key, span);
        }
        return true;
    }
    /// <summary>
    /// 判断指定rediskey是否存在
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="cacheType">Object：0，Hash：1；SortedSet：2。兼容net46下的操作，不然可以不用</param>
    /// <param name="server"></param>
    /// <returns></returns>
    private Task<bool> KeyExists<T>(string key, int cacheType, IServerOptions server)
    {
        RedisKey redisKey = BuildKey(typeof(T), key, cacheType);
        return GetDatabase(server).KeyExistsAsync(redisKey);
    }
    /// <summary>
    /// 根据Key移除指定的redis数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="keys"></param>
    /// <param name="cacheType">Object：0，Hash：1；SortedSet：2。兼容net46下的操作，不然可以不用</param>
    /// <param name="server"></param>
    /// <returns></returns>
    private async Task<bool> KeyRemove<T>(IList<string> keys, int cacheType, IServerOptions server)
    {
        ThrowIfNullOrEmpty(keys, "keys为null或者空集合");
        Type type = typeof(T);
        RedisKey[] redisKeys = keys.Select(item => BuildKey(type, item, cacheType)).ToArray();
        //  避免出现key不存在，还移除时数量对不上；不报错则为true
        await _manager.GetDatabase(server, 0).KeyDeleteAsync(redisKeys);
        return true;
    }
    #endregion
}
