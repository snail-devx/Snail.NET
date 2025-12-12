using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Threading.Extensions;
using System.Diagnostics;

namespace Snail.Utilities.Collections;
/// <summary>
/// 加锁列表
/// <para>1、线程安全，支持多线程读写 </para>
/// <para>2、提供简单的读取和写入、遍历逻辑。仅满足自身业务需求，不建议对外大量使用 </para>
/// </summary>
/// <typeparam name="T">列表数据类型，仅支持引用数据类型</typeparam>
/// <remarks>
/// 注意事项：
/// <para>1、 内部使用<see cref="List{T}"/>完成数据存储 </para>
/// </remarks>
public sealed class LockList<T> : Disposable, IDisposable where T : notnull
{
    #region 属性变量
    /// <summary>
    /// 读写锁
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ReaderWriterLockSlim _lock = new();
    /// <summary>
    /// 实际存储数据的列表
    /// </summary>
    private readonly List<T> _items = new();

    /// <summary>
    /// 列表长度
    /// </summary>
    public int Count => _items.Count;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public LockList() { }
    #endregion

    #region 公共方法

    #region 添加
    /// <summary>
    /// 添加数据
    /// </summary>
    /// <param name="obj">要添加的数据；为null报错</param>
    /// <returns>返回自身；方便链式调用</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public LockList<T> Add(in T obj)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(obj);
        _lock.RunInWrite(_items.Add, obj);
        return this;
    }
    /// <summary>
    /// 批量添加数据
    /// </summary>
    /// <param name="objs"></param>
    /// <returns></returns>
    public LockList<T> AddRange(in LockList<T> objs)
        => AddRange(objs._items);
    /// <summary>
    /// 批量添加数据
    /// </summary>
    /// <param name="objs">要添加的数据列表；存在为null的数据报错</param>
    /// <returns>返回自身；方便链式调用</returns>
    /// <exception cref="ArgumentException"></exception>
    public LockList<T> AddRange(in IList<T> objs)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (objs?.Any() == true)
        {
            ThrowIfTrue(objs.Any(item => item == null), $"{nameof(objs)}存在为null的数据");
            _lock.RunInWrite(_items.AddRange, objs);
        }
        return this;
    }
    /// <summary>
    /// 批量添加数据
    /// </summary>
    /// <param name="objs">要添加的数据列表</param>
    /// <returns>返回自身；方便链式调用</returns>
    /// <exception cref="ArgumentException"></exception>
    public LockList<T> AddRange(in IEnumerable<T> objs)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (objs?.Any() == true)
        {
            ThrowIfTrue(objs.Any(item => item == null), $"{nameof(objs)}存在为null的数据");
            _lock.RunInWrite(_items.AddRange, objs);
        }
        return this;
    }
    /// <summary>
    /// 把符合条件数据干掉，并把新数据加进去
    /// </summary>
    /// <param name="predict">断言，替换哪些数据</param>
    /// <param name="obj">要替换的数据，为null报错</param>
    /// <remarks>完全是想节省一次写锁加入，否则先removeall，再add即可</remarks>
    /// <returns>返回自身；方便链式调用</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public LockList<T> Replace(in Predicate<T> predict, in T obj)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predict);
        ThrowIfNull(obj);
        _lock.RunInWrite((predict, obj) =>
        {
            _items.RemoveAll(predict);
            _items.Add(obj);
        }, predict, obj);
        return this;
    }
    #endregion

    #region 查找
    /// <summary>
    /// 判断是否存在符合条件的数据
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public bool Any(in Func<T, bool> predicate)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predicate);
        return _lock.RunInRead(_items.Any, predicate);
    }
    /// <summary>
    /// 获取数据；等价于FirstOrDefault
    /// </summary>
    /// <param name="predicate">查找数据的断言</param>
    /// <param name="isDescending">从前往后查，还是从后往前查。默认从前往后</param>
    /// <returns></returns>
    public T? Get(in Func<T, bool> predicate, in bool isDescending)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predicate);
        Func<Func<T, bool>, T?> func = isDescending ? _items.LastOrDefault : _items.FirstOrDefault;
        return _lock.RunInRead(func, predicate);
    }
    /// <summary>
    /// 获取符合条件的所有数据
    /// </summary>
    /// <param name="predicate">查找数据的断言</param>
    /// <returns></returns>
    public IList<T> GetAll(in Func<T, bool> predicate)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predicate);
        return _lock.RunInRead(
            predicate => _items.Where(predicate).ToList(),
            predicate
        );
    }
    /// <summary>
    /// 获取数据，不存在时添加
    /// </summary>
    /// <param name="predicate">查找数据的断言</param>
    /// <param name="addFunc">构建实例数据委托；数据查找失败时，调用此委托构建新数据加入</param>
    /// <returns></returns>
    public T? GetOrAdd(in Predicate<T> predicate, in Func<T?> addFunc)
        => GetOrAdd(predicate, false, addFunc);
    /// <summary>
    /// 获取数据，不存在时添加
    /// </summary>
    /// <param name="predicate">查找数据的断言</param>
    /// <param name="isDescending">从前往后查，还是从后往前查。默认从前往后</param>
    /// <param name="addFunc">构建实例数据委托；数据查找失败时，调用此委托构建新数据加入</param>
    /// <returns></returns>
    public T? GetOrAdd(in Predicate<T> predicate, in bool isDescending, in Func<T?> addFunc)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predicate);
        ThrowIfNull(addFunc);
        //  查数据，查找不到则构建新数据；构建出来的新数据非null，则追加
        T? value = default;
        _lock.RunInUpgrade((predicate, isDescending, addFunc) =>
        {
            int index = isDescending == true ? _items.LastIndexOf(predicate) : _items.IndexOf(predicate);
            value = index == -1 ? addFunc.Invoke() : _items[index];
            if (index == -1 && value != null)
            {
                Add(value);
            }
        }, predicate, isDescending, addFunc);
        return value;
    }
    #endregion

    #region 遍历、转换
    /// <summary>
    /// 遍历数据
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public void Foreach(in Action<T> action)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(action);
        _lock.RunInRead(_items.ForEach, action);
    }

    /// <summary>
    /// 可枚举对象
    /// </summary>
    /// <returns></returns>
    public IEnumerable<T> AsEnumerable()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _items.AsEnumerable();
    }

    /// <summary>
    /// 将实例组装成通用List集合
    /// </summary>
    /// <returns></returns>
    public IList<T> ToList()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        return _lock.RunInRead(_items.ToList);
    }
    #endregion

    #region 移除
    /// <summary>
    /// 移除符合条件的数据第一个数据
    /// </summary>
    /// <param name="predict">查找数据的断言</param>
    /// <param name="isDescending">从前往后查，还是从后往前查。默认从前往后</param>
    /// <returns></returns>
    public LockList<T> Remove(in Func<T, bool> predict, in bool isDescending = false)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        T? obj = Get(predict, isDescending);
        if (obj != null) Remove(obj);
        return this;
    }
    /// <summary>
    /// 移除指定数据
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public void Remove(in T obj)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(obj);
        _lock.RunInWrite(_items.Remove, obj);
    }
    /// <summary>
    /// 移除符合条件的所有数据
    /// </summary>
    /// <param name="predicate">查找数据的断言</param>
    public void RemoveAll(in Predicate<T> predicate)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ThrowIfNull(predicate);
        _lock.RunInWrite(_items.RemoveAll, predicate);
    }

    /// <summary>
    /// 清空字典
    /// </summary>
    public void Clear()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _lock.RunInWrite(_items.Clear);
    }
    #endregion

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
                _lock.Dispose();
                _items.Clear();
                _items.TryDispose();
            }
            // TODO: 释放未托管的资源(未托管的对象)并重写终结器
            // TODO: 将大型字段设置为 null
        }
        //  执行基类回收
        base.Dispose(disposing);
    }
    #endregion
}
