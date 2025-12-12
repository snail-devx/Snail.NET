using Snail.Abstractions.Common.Interfaces;
using Snail.Common.Utils;
using Snail.Utilities.Collections;

namespace Snail.Common.Components;

/// <summary>
/// 对象池：管理对象的构建，回收 
/// </summary>
public sealed class ObjectPool<T> : Disposable where T : class, IPoolObject
{
    #region 属性变量
    /// <summary>
    /// 池中对象
    /// </summary>
    private readonly LockList<T> _items = new LockList<T>();
    /// <summary>
    /// 闲置时间间隔：超过此间隔时间的闲置对象自动回收掉
    /// </summary>
    private readonly TimeSpan _idleInterval;

    /// <summary>
    /// 对象数量
    /// </summary>
    public int Count => _items.Count;
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    public ObjectPool(TimeSpan idleInterval)
    {
        ThrowIfFalse(idleInterval.TotalSeconds > 0, $"{nameof(idleInterval)}最小单位为秒（s）");
        _idleInterval = idleInterval;
        InternalTimer.OnRun += OnRun_ClearIdleObject;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取空闲对象，无则创建新的
    /// </summary>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <returns>空闲对象，自动执行<see cref="IPoolObject.Using"/>，标记为using状态；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public T GetOrAdd(Func<T> addFunc)
        => GetOrAdd(predicate: null, addFunc, autoUsing: true);
    /// <summary>
    /// 获取符合条件对象，无则创建新的
    /// </summary>
    /// <param name="predicate">为null则获取空闲对象</param>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <param name="autoUsing">是否自动执行<see cref="IPoolObject.Using"/>方法，将对象标记为使用状态；推荐true，外部手动using，需要做好并发管理</param>
    /// <returns>空闲对象；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public T GetOrAdd(Func<T, bool>? predicate, Func<T> addFunc, bool autoUsing = true)
    {
        ThrowIfNull(addFunc);
        T item = _items.GetOrAdd(
            predicate: obj =>
            {
                bool isIdle = predicate == null ? obj.IsIdle : predicate(obj);
                RunIf(isIdle && autoUsing, obj.Using);
                return isIdle;
            },
            addFunc: () =>
            {
                T obj = addFunc();
                RunIf(autoUsing, obj.Using);
                return obj;
            }
         )!;
        return item;
    }
    #endregion

    #region 继承方法
    /// <summary>
    /// 对象释放
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed == false)
        {
            if (disposing)
            {
                // TODO: 释放托管状态(托管对象)
                InternalTimer.OnRun -= OnRun_ClearIdleObject;
                _items.TryDispose();
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 运行程序，执行清理闲置对象
    /// </summary>
    private void OnRun_ClearIdleObject()
    {
        if (_items.Count == 0)
        {
            return;
        }
        //  加锁遍历；找哪些对象需要回收
        IList<T> deletes = new List<T>();
        _items.RemoveAll(proxy =>
        {
            bool needRecycle = proxy.IsIdle && DateTime.UtcNow.Subtract(proxy.IdleTime) > _idleInterval;
            RunIf(needRecycle, deletes.Add, proxy);
            return needRecycle;
        });
        //  移除对象，尝试销毁
        deletes.ForEach(ObjectExtensions.TryDispose);
    }
    #endregion
}
