using Snail.Dependency.Interfaces;
using Snail.Utilities.Threading.Extensions;

namespace Snail.Dependency.Components
{
    /// <summary>
    /// 单例生命周期示例保存器；支持全局单例和实例单例<br />
    ///     1、全局单例，生命周期<see cref="LifetimeType.Singleton"/> <br />
    ///     2、容器单例，生命周期<see cref="LifetimeType.Scope"/> 
    /// </summary>
    internal sealed class SingleStorager : ITypeStorager
    {
        #region 属性变量
        /// <summary>
        /// 是否是容器单例
        /// </summary>
        private readonly bool _isScopeSingle;
        /// <summary>
        /// 加锁对象，禁止【递归】访问
        /// </summary>
        private readonly ReaderWriterLockSlim _lock;
        /// <summary>
        /// 依赖注入构建的实例值
        /// </summary>
        private object? _value;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="manager">管理器实例，用于监听<see cref="IDIManager.OnDestroy"/>事件</param>
        /// <param name="isScopeSingle">是否是容器单例</param>
        public SingleStorager(in IDIManager manager, in bool isScopeSingle)
        {
            _isScopeSingle = isScopeSingle;
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        }
        #endregion

        #region IInstaceStorager
        /// <summary>
        /// 基于当前存储器，构建新的存储器实例<br />
        ///     1、在<see cref="IDIManager.New"/>执行时，会依赖注入的实例存储器做继承处理，从而维护完整的生命周期
        /// </summary>
        /// <returns>新的存储器实例，若无需继承，则返回null即可</returns>
        /// <remarks>容器单例不继承，否则继承</remarks>
        ITypeStorager? ITypeStorager.New() => _isScopeSingle ? null : this;

        /// <summary>
        /// 获取依赖实例对象
        /// </summary>
        /// <returns>若返回null，则DI需要全新构建</returns>
        object? ITypeStorager.GetInstace()
        {
            /*
             *  尽可能从性能角度出发
             *      1、若实例非null直接返回，不用重新获取
             *      2、若实例为null则加锁再获取一下
             *          1、值为null则返回，让外部自动构建实例；构建完成后进入saveinstance逻辑，然后解锁
             *          2、值非null则解锁返回
             */
            //  _value无值，则进入【可升级写】锁等待【SaveInstace】方法保存值；有值时则退出升级锁
            if (_value == null)
            {
                _lock.EnterUpgradeableReadLock();
                if (_value != null)
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }
            return _value;
        }

        /// <summary>
        /// 保存实例对象
        /// </summary>
        /// <param name="instance">DI构建的实例对象</param>
        void ITypeStorager.SaveInstace(in Object? instance)
        {
            //  仅保存非null值；做一个冗余，若进入写锁后已经有值了，报错（理论上不可能存在，图个心安）
            if (instance != null)
            {
                _lock.RunInWrite(instance =>
                {
                    ThrowIfNotNull(_value, $"{GetType().Name}：保存实例方法不能重复调用");
                    _value = instance;
                }, instance);
            }
            //  【可升级写】锁解锁，和GetInstace呼应
            if (_lock.IsUpgradeableReadLockHeld == true)
            {
                _lock.ExitUpgradeableReadLock();
            }
        }
        /// <summary>
        /// 尝试实例销毁存储器
        /// </summary>
        void ITypeStorager.TryDestroy()
        {
        }
        #endregion
    }
}
