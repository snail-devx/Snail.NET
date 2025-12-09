using Snail.Abstractions.Common.Interfaces;
using Snail.Common.Utils;
using Snail.Utilities.Threading;

namespace Snail.Common.Components;

/// <summary>
/// 对象异步池：管理对象的构建，回收 <br />
///     1、对象的复用条件支持异步断言 <br />
///     2、对象的创建支持异步 <br />
/// </summary>
/// <remarks>和<see cref="ObjectPool{T}"/>功能相似，加锁机制不通用</remarks>
public sealed class ObjectAsyncPool<T> : Disposable where T : class, IPoolObject
{
    #region 属性变量
    /// <summary>
    /// 异步锁，支持代码块中含有await异步方法执行
    /// </summary>
    private readonly AsyncLock _lock = new AsyncLock();
    /// <summary>
    /// 池中对象
    /// </summary>
    private readonly List<T> _items = new List<T>();
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
    public ObjectAsyncPool(TimeSpan idleInterval)
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
        => GetOrAdd(predicate: (Func<T, bool>?)null, addFunc, autoUsing: true);
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
        using (_lock.Wait())
        {
            T? proxy = predicate == null
                ? _items.FirstOrDefault(proxy => proxy.IsIdle)
                : _items.FirstOrDefault(proxy => predicate(proxy));
            proxy ??= addFunc().AddTo(_items);
            RunIf(autoUsing, proxy.Using);
            return proxy;
        }
    }

    /// <summary>
    /// 获取空闲对象，无则创建新的
    /// </summary>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <returns>空闲对象，自动执行<see cref="IPoolObject.Using"/>，标记为using状态；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public Task<T> GetOrAdd(Func<Task<T>> addFunc)
        => GetOrAdd(predicate: (Func<T, bool>?)null, addFunc, autoUsing: true);
    /// <summary>
    /// 获取符合条件对象，无则创建新的
    /// </summary>
    /// <param name="predicate">为null则获取空闲对象</param>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <param name="autoUsing">是否自动执行<see cref="IPoolObject.Using"/>方法，将对象标记为使用状态；推荐true，外部手动using，需要做好并发管理</param>
    /// <returns>空闲对象；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public async Task<T> GetOrAdd(Func<T, bool>? predicate, Func<Task<T>> addFunc, bool autoUsing = true)
    {
        ThrowIfNull(addFunc);
        using (await _lock.Await())
        {
            T? proxy = predicate == null
               ? _items.FirstOrDefault(proxy => proxy.IsIdle)
               : _items.FirstOrDefault(predicate);
            if (proxy == null)
            {
                proxy = await addFunc();
                _items.Add(proxy);
            }
            RunIf(autoUsing, proxy.Using);
            return proxy;
        }
    }
    /// <summary>
    /// 获取符合条件对象，无则创建新的
    /// </summary>
    /// <param name="predicate">为null则获取空闲对象</param>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <param name="autoUsing">是否自动执行<see cref="IPoolObject.Using"/>方法，将对象标记为使用状态；推荐true，外部手动using，需要做好并发管理</param>
    /// <returns>空闲对象；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public async Task<T> GetOrAdd(Func<T, Task<bool>> predicate, Func<T> addFunc, bool autoUsing = true)
    {
        ThrowIfNull(addFunc);
        using (await _lock.Await())
        {
            T? proxy = predicate == null
                ? _items.FirstOrDefault(item => item.IsIdle)
                : await FirstOrDefaultAsync(predicate);
            proxy ??= addFunc().AddTo(_items);
            RunIf(autoUsing, proxy.Using);
            return proxy;
        }
    }
    /// <summary>
    /// 获取符合条件对象，无则创建新的
    /// </summary>
    /// <param name="predicate">为null则获取空闲对象</param>
    /// <param name="addFunc">创建新对象的委托方法</param>
    /// <param name="autoUsing">是否自动执行<see cref="IPoolObject.Using"/>方法，将对象标记为使用状态；推荐true，外部手动using，需要做好并发管理</param>
    /// <returns>空闲对象；使用完成后执行<see cref="IPoolObject.Used"/>方法</returns>
    public async Task<T> GetOrAdd(Func<T, Task<bool>>? predicate, Func<Task<T>> addFunc, bool autoUsing = true)
    {
        ThrowIfNull(addFunc);
        using (await _lock.Await())
        {
            T? item = predicate == null
                ? _items.FirstOrDefault(item => item.IsIdle)
                : await FirstOrDefaultAsync(predicate);
            if (item == null)
            {
                item = await addFunc();
                _items.Add(item);
            }
            RunIf(autoUsing, item.Using);
            return item;
        }
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
                _lock.TryDispose();
                _items.Clear();
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
    /// 异步查找是否有符合条件的第一个数据
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    private async Task<T?> FirstOrDefaultAsync(Func<T, Task<bool>> predicate)
    {
        foreach (var item in _items)
        {
            if (await predicate(item) == true)
            {
                return item;
            }
        }
        return null;
    }

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
        using (_lock.Wait())
        {
            _items.RemoveAll(proxy =>
            {
                bool needRecycle = proxy.IsIdle && DateTime.UtcNow.Subtract(proxy.IdleTime) > _idleInterval;
                RunIf(needRecycle, deletes.Add, proxy);
                return needRecycle;
            });
        }
        //  移除对象，尝试销毁
        deletes.ForEach(item => item.Dispose());
    }
    #endregion
}
