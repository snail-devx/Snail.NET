using Snail.Abstractions.Distribution.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using StackExchange.Redis;

namespace Snail.Redis;

/// <summary>
/// Redis实现的锁提供程序
/// </summary>
[Component<ILockProvider>]
[Component<ILockProvider>(Key = DIKEY_Redis)]
public sealed class RedisLockProvider : ILockProvider
{
    #region 属性变量
    /// <summary>
    /// 默认过期时间：10分钟
    /// </summary>
    private const long DEFAULT_ExpireSeconds = 10 * 60;
    /// <summary>
    /// Redis管理器
    /// </summary>
    private readonly RedisManager _manager;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="manager"></param>
    public RedisLockProvider(RedisManager manager)
    {
        _manager = ThrowIfNull(manager);
    }
    #endregion

    #region ILockProvider
    /// <summary>
    /// 加锁
    /// </summary>
    /// <param name="key">加锁的Key；确保唯一</param>
    /// <param name="value">锁的值；在释放锁时使用；只有值正确才能被释放掉</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；为null默认20次；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥锁</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <param name="server">加锁服务器配置选项</param>
    /// <returns>加锁成功返回true；否则返回false</returns>
    async Task<bool> ILockProvider.Lock(string key, string value, uint? maxTryCount, long? expireSeconds, IServerOptions server)
    {
        //  默认值处理，加锁信息初始化
        maxTryCount = Math.Min(maxTryCount ?? 20, 400);
        if (expireSeconds == null || expireSeconds == 0)
        {
            expireSeconds = DEFAULT_ExpireSeconds;
        }
        TimeSpan expire = FromSeconds(expireSeconds.Value);
        RedisKey lockKey = BuildLockKey(key);
        RedisValue lockValue = value;
        IDatabase db = _manager.GetDatabase(server, dbIndex: 1);
        //  尝试加锁；加锁失败睡眠100ms重试
        while (await db.LockTakeAsync(lockKey, lockValue, expire) == false)
        {
            //  超过最大重视次数，加锁失败
            if (maxTryCount == 0)
            {
                return false;
            }
            //  等待后，继续加锁
            await Task.Delay(100);
            maxTryCount -= 1;
        }
        //  走到这里都是加锁成功了
        return true;
    }
    /// <summary>
    /// 解锁
    /// </summary>
    /// <param name="key">加锁的Key</param>
    /// <param name="value">锁的值；加锁时传入的锁值</param>
    /// <param name="server">加锁服务器配置选项</param>
    /// <returns>解锁成功返回true；否则返回false</returns>
    Task<bool> ILockProvider.Unlock(string key, string value, IServerOptions server)
    {
        RedisKey lockKey = BuildLockKey(key);
        IDatabase db = _manager.GetDatabase(server, dbIndex: 1);
        return db.LockReleaseAsync(lockKey, value);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建加锁的key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static RedisKey BuildLockKey(String key)
    {
        ThrowIfNullOrEmpty(key);
        return $"DistributedLock:{key}";
    }
    #endregion
}
