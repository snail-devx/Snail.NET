namespace Snail.Utilities.Threading
{
    /// <summary>
    /// 异步锁；用于“代码块中存在await异步方法调用时”的并发控制
    /// </summary>
    public sealed class AsyncLock
    {
        #region 属性变量
        /// <summary>
        /// 信号锁
        /// </summary>
        private readonly SemaphoreSlim _slim = new SemaphoreSlim(1, maxCount: 1);
        #endregion

        #region 公共方法
        /// <summary>
        /// 同步等待进入锁作用域
        /// </summary>
        /// <remarks>外部请使用using进行锁自动释放</remarks>
        /// <returns></returns>
        public LockScope Wait()
        {
            _slim.Wait();
            return GetScope();
        }
        /// <summary>
        /// 异步等待进入锁作用域；用于async方法中控制并发
        /// </summary>
        /// <remarks>外部请使用using进行锁自动释放</remarks>
        /// <returns></returns>
        public async Task<LockScope> Await()
        {
            await _slim.WaitAsync();
            return GetScope();
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取锁所用域
        /// </summary>
        /// <returns></returns>
        private LockScope GetScope()
        {
            var scope = new LockScope();
            scope.OnDestroy += () => _slim.Release();
            return scope;
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 锁的作用域
        /// </summary>
        public class LockScope : IDisposable
        {
            #region 属性变量
            /// <summary>
            /// 事件：销毁时
            /// </summary>
            public event Action? OnDestroy;
            #endregion

            #region IDisposable
            /// <summary>
            /// 销毁
            /// </summary>
            public void Dispose()
            {
                OnDestroy?.Invoke();
                OnDestroy = null;
            }
            #endregion
        }
        #endregion
    }
}
