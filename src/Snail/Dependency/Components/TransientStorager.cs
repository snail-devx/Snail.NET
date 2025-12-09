using Snail.Dependency.Interfaces;

namespace Snail.Dependency.Components;

/// <summary>
/// 瞬时生命周期的实例保存器
/// </summary>
/// <remarks>后续考虑，若不存储保存示例，这里给一个Empty示例，用于所有【瞬时】生命周期依赖注入的存储器</remarks>
internal sealed class TransientStorager : ITypeStorager
{
    #region 属性变量
    /// <summary>
    /// 读写锁
    /// </summary>
    private readonly ReaderWriterLockSlim _lock;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="manager">管理器实例，用于监听<see cref="IDIManager.OnDestroy"/>事件</param>
    public TransientStorager(in IDIManager manager)
    {
        //  禁止递归，避免瞬时的死循环
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    }
    #endregion

    #region IInstaceStorager
    /// <summary>
    /// 基于当前存储器，构建新的存储器实例<br />
    ///     1、在<see cref="IDIManager.New"/>执行时，会依赖注入的实例存储器做继承处理，从而维护完整的生命周期
    /// </summary>
    /// <returns>新的存储器实例，若无需继承，则返回null即可</returns>
    ITypeStorager? ITypeStorager.New() => null;

    /// <summary>
    /// 获取依赖实例对象
    /// </summary>
    /// <returns>若返回null，则DI需要全新构建</returns>
    Object? ITypeStorager.GetInstace()
    {
        //  加读锁，避免同一个线程递归进入
        _lock.EnterReadLock();

        return null;
    }

    /// <summary>
    /// 保存实例对象
    /// </summary>
    /// <param name="instance">DI构建的实例对象</param>
    void ITypeStorager.SaveInstace(in Object? instance)
    {
        //  保存时暂时不做任何操作，后期看情况保存构建过的示例
        ///// <summary>
        ///// 此生命周期策略类构建过的实例，销毁时做自动销毁
        ///// </summary>
        // private readonly List<Object> _values = new();

        //  解除读锁
        if (_lock.IsReadLockHeld == true)
        {
            _lock.ExitReadLock();
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
