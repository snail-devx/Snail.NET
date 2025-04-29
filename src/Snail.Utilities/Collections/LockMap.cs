using System.Diagnostics;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Threading.Extensions;

namespace Snail.Utilities.Collections
{
    /// <summary>
    /// 加锁键值字典类 <br />
    ///     1、线程安全，优化ConcurrentDictionary.GetOrAdd委托多次调用的问题 <br />
    ///     2、提供简单的读取和写入、遍历逻辑。仅满足自身业务需求，不建议对外大量使用 <br />
    /// </summary>
    /// <remarks>
    /// 注意事项： <br />
    ///     1、使用<see cref="Dictionary{TKey, TValue}"/>做数据存储，Key无序；后续考虑优化数据结构，实现Key有序 <br />
    /// </remarks>
    public sealed class LockMap<TKey, TValue> : Disposable, IDisposable where TKey : notnull
    {
        #region 属性变量
        /// <summary>
        /// 读写锁
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ReaderWriterLockSlim _lock = new();
        /// <summary>
        /// 实际存储数据的字典
        /// </summary>
        private readonly Dictionary<TKey, TValue> _dict;

        /// <summary>
        /// 字典长度
        /// </summary>
        public int Count => _dict.Count;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        public LockMap() : this(dict: null)
        {
        }
        /// <summary>
        /// 构造方法，基于字典数据构建
        ///     不对外开放，避免外部引用操作
        /// </summary>
        /// <param name="dict"></param>
        private LockMap(in Dictionary<TKey, TValue>? dict)
        {
            _dict = [];
            dict?.ForEach((key, value) => _dict[key] = value);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 获取指定Key的数据，不存在时执行添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="addFunc">对应Key不存在时，调用此委托构建新值存入字典中</param>
        /// <returns></returns>
        public TValue GetOrAdd(in TKey key, in Func<TKey, TValue> addFunc)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            ThrowIfNull(addFunc);
            //  模拟lock加两次锁，做到绝对一致
            if (_dict.TryGetValue(key, out TValue? value) != true)
            {
                _lock.RunInWrite((key, addFunc) =>
                {
                    if (_dict.TryGetValue(key, out value) != true)
                    {
                        value = addFunc.Invoke(key);
                        _dict[key] = value;
                    }
                }, key, addFunc);
            }
            //  返回值：已经确保会有数据了，无数据也会调用addFunc方法
            return value!;
        }
        /// <summary>
        /// 获取指定Key的数据，不存在则执行添加
        /// </summary>
        /// <typeparam name="Param"></typeparam>
        /// <param name="key"></param>
        /// <param name="addFunc">添加值的委托</param>
        /// <param name="param1">传递给<paramref name="addFunc"/>的参数</param>
        /// <returns></returns>
        public TValue GetOrAdd<Param>(in TKey key, in Func<Param, TValue> addFunc, Param param1)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            ThrowIfNull(addFunc);
            //  模拟lock加两次锁，做到绝对一致
            if (_dict.TryGetValue(key, out TValue? value) != true)
            {
                _lock.RunInWrite((key, addFunc) =>
                {
                    if (_dict.TryGetValue(key, out value) != true)
                    {
                        value = addFunc.Invoke(param1);
                        _dict[key] = value;
                    }
                }, key, addFunc);
            }
            //  返回值：已经确保会有数据了，无数据也会调用addFunc方法
            return value!;
        }

        /// <summary>
        /// 设置key、value值；存在替换，不存在添加
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(in TKey key, in TValue value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            /*  减少匿名委托使用，替换成下面三行代码
                _lock.RunInWrite(() => _dict[key] = value);
             */
            _lock.EnterWriteLock();
            _dict[key] = value;
            _lock.ExitWriteLock();
        }

        /// <summary>
        /// 移除指定Key数据
        /// </summary>
        /// <param name="key"></param>
        public void Remove(in TKey key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            _lock.RunInWrite(_dict.Remove, key);
        }
        /// <summary>
        /// 移除指定Key数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">移除的数据值</param>
        /// <returns>移除成功返回true；否则false</returns>
        public bool Remove(in TKey key, out TValue? value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            /*  减少匿名方法使用，替换代码
                Boolean bv = false;
                value = _lock.RunInWrite(() =>
                {
                    bv = _dict.Remove(key, out TValue? rv);
                    return rv;
                });
                return bv;
             */
            _lock.EnterWriteLock();
            bool bvalue = _dict.Remove(key, out value);
            _lock.ExitWriteLock();
            return bvalue;
        }

        /// <summary>
        /// 清空字典
        /// </summary>
        public void Clear()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            //_lock.RunInWrite(() => _dict.Clear());
            _lock.RunInWrite(_dict.Clear);
        }

        /// <summary>
        /// 尝试获取指定key数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(in TKey key, out TValue? value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            return _dict.TryGetValue(key, out value);
        }
        /// <summary>
        /// 字典中是否存在指定Key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasKey(in TKey key)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            ThrowIfNull(key);
            return _dict.ContainsKey(key);
        }
        /// <summary>
        /// 字典中是否存在指定Value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool HasValue(in TValue value)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return _dict.ContainsValue(value);
        }

        /// <summary>
        /// 是否有数据
        /// </summary>
        /// <returns></returns>
        public bool Any()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return _dict.Count > 0;
        }

        /// <summary>
        /// 遍历字典：无序，不可中断遍历
        /// </summary>
        /// <param name="each"></param>
        public void ForEach(in Action<TKey, TValue> each)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            /*_dict.ForEach为扩展方法，参数有in约束，无法直接作为action传入到RunInRead中，否则可简写成
             * _lock.RunInRead(_dict.ForEach, each);
             */

            //  上读锁；解决直接foreach遍历dictionary时，同步有写入会报错的问题
            ThrowIfNull(each);
            _lock.RunInRead(each => _dict.ForEach(each), each);
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
                    _lock.Dispose();
                    _dict.Clear();
                    _dict.TryDispose();
                }
                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
            }
            //  执行基类回收
            base.Dispose(disposing);
        }
        #endregion
    }
}
