using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Collections;
using Snail.Utilities.Common.Extensions;

namespace Snail.Identity.Components
{
    /// <summary>
    /// 雪花算法主键Id管理器
    /// </summary>
    /// <remarks>作为默认<see cref="IIdProvider"/>实现类</remarks>
    [Component<IIdProvider>(Lifetime = LifetimeType.Singleton)]
    [Component<IIdProvider>(Lifetime = LifetimeType.Singleton, Key = DIKEY_SnowFlake)]
    public sealed class SnowFlakeIdProvider : IIdProvider
    {
        #region 属性变量
        /// <summary>
        /// 主键Id生成器缓存。key为数据中心Id和workid值；value为对应的IdWorker实例
        /// </summary>
        private static readonly LockMap<string, SnowFlakeIdWorker> _idWorkers = new();
        /// <summary>
        /// Id生成时的锁变量
        /// 目的：不管外部怎么是多个实例，还是多个线程过来，都得锁住了
        /// </summary>
        private static readonly object _IdLockVar = new();
        /// <summary>
        /// 应用程序实例
        /// </summary>
        private readonly IApplication _app;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app">应用程序实例</param>
        public SnowFlakeIdProvider(IApplication app)
        {
            _app = ThrowIfNull(app);
        }
        #endregion

        #region IIdProvider
        /// <summary>
        /// 生成新的主键Id
        /// </summary>
        /// <param name="codeType">>编码类型；默认Default；Provider中可根据此做id区段区分；具体得看实现类是否支持</param>
        /// <param name="server">服务器配置选项；为null提供程序自身做默认值处理，或者报错</param>
        /// <returns></returns>
        string IIdProvider.NewId(string? codeType, IServerOptions? server)
        {
            SnowFlakeIdWorker worker = BuildWorker(_app);
            //  后期这里可以考虑做优化，不加锁，在worker中基于当前时间Tick做动态锁试试；但要考虑多线程下的并行
            lock (_IdLockVar)
            {
                string id = worker.NextId().ToString();
                return id;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 构建Id生成器
        /// </summary>
        /// <returns></returns>
        private static SnowFlakeIdWorker BuildWorker(IApplication app)
        {
            /*后期支持配置开始时间 _twepoch*/

            int datacenterId = app.GetEnv("DatacenterId")?.AsInt32() ?? 0;
            int workerId = app.GetEnv("WorkerId")?.AsInt32() ?? 0;
            return _idWorkers.GetOrAdd(
                $"{datacenterId}:{workerId}",
                key => new SnowFlakeIdWorker(datacenterId, workerId)
            );
        }
        #endregion
    }
}
