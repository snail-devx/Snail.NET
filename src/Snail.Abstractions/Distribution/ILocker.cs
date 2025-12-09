namespace Snail.Abstractions.Distribution;

/// <summary>
/// 接口约束：分布式加锁器
/// </summary>
public interface ILocker
{
    /// <summary>
    /// 加锁
    /// </summary>
    /// <param name="key">加锁的Key；确保唯一</param>
    /// <param name="value">锁的值；在释放锁时使用；只有值正确才能被释放掉</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；为null默认20次；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥锁</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>加锁成功返回true；否则返回false</returns>
    Task<bool> Lock(string key, string value, uint maxTryCount = 20, int expireSeconds = 60);

    /// <summary>
    /// 解锁
    /// </summary>
    /// <param name="key">加锁的Key</param>
    /// <param name="value">锁的值；加锁时传入的锁值</param>
    /// <returns>解锁成功返回true；否则返回false</returns>
    Task<bool> Unlock(string key, string value);
}
