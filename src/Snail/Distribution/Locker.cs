using Snail.Abstractions.Distribution;
using Snail.Abstractions.Distribution.Interfaces;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Distribution
{
    /// <summary>
    /// 分布式锁
    /// </summary>
    [Component<ILocker>(Lifetime = LifetimeType.Transient)]
    public sealed class Locker : ILocker
    {
        #region 属性变量
        /// <summary>
        /// 分布式锁服务器配置选项
        /// </summary>
        private readonly IServerOptions _server;
        /// <summary>
        /// 锁实现提供程序
        /// </summary>
        private readonly ILockProvider _provider;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="server">缓存服务器配置选项</param>
        /// <param name="provider">缓存提供程序，用于实际读写缓存</param>
        public Locker(IServerOptions server, ILockProvider? provider)
        {
            _server = ThrowIfNull(server);
            _provider = ThrowIfNull(provider);
        }
        #endregion

        #region ILocker
        /// <summary>
        /// 加锁
        /// </summary>
        /// <param name="key">加锁的Key；确保唯一</param>
        /// <param name="value">锁的值；在释放锁时使用；只有值正确才能被释放掉</param>
        /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
        /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
        /// <returns>加锁成功返回true；否则返回false</returns>
        Task<bool> ILocker.Lock(string key, string value, uint maxTryCount, int expireSeconds)
            => _provider.Lock(key, value, maxTryCount, expireSeconds, _server);
        /// <summary>
        /// 解锁
        /// </summary>
        /// <param name="key">加锁的Key</param>
        /// <param name="value">锁的值；加锁时传入的锁值</param>
        /// <returns>解锁成功返回true；否则返回false</returns>
        Task<bool> ILocker.Unlock(string key, string value)
            => _provider.Unlock(key, value, _server);
        #endregion
    }
}
