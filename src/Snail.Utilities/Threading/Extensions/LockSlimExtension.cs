namespace Snail.Utilities.Threading.Extensions;
/// <summary>
/// 读写锁扩展
/// </summary>
public static class LockSlimExtension
{
    #region 读操作
    /// <summary>
    /// 在“读锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是读；多线程并发读取 </para>
    /// </summary>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    public static void RunInRead(this ReaderWriterLockSlim lockSlim, in Action action)
    {
        ThrowIfNull(action);
        lockSlim.EnterReadLock();
        try { action.Invoke(); }
        finally { lockSlim.ExitReadLock(); }
    }
    /// <summary>
    /// 在“读锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是读；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    public static void RunInRead<T>(this ReaderWriterLockSlim lockSlim, in Action<T> action, in T param1)
    {
        ThrowIfNull(action);
        lockSlim.EnterReadLock();
        try { action.Invoke(param1); }
        finally { lockSlim.ExitReadLock(); }
    }
    /// <summary>
    /// 在“读锁”环境下运行委
    /// <para>1、使用场景：所有action操作都是读；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    /// <param name="param2"><paramref name="action"/>的第二个参数</param>
    public static void RunInRead<T1, T2>(this ReaderWriterLockSlim lockSlim, in Action<T1, T2> action, in T1 param1, in T2 param2)
    {
        ThrowIfNull(action);
        lockSlim.EnterReadLock();
        try { action.Invoke(param1, param2); }
        finally { lockSlim.ExitReadLock(); }
    }

    /// <summary>
    /// 在“读锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是读，但有返回值；多线程并发读取 </para>
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <returns></returns>
    public static TResult RunInRead<TResult>(this ReaderWriterLockSlim lockSlim, in Func<TResult> func)
    {
        ThrowIfNull(func);
        lockSlim.EnterReadLock();
        try { return func.Invoke(); }
        finally { lockSlim.ExitReadLock(); }
    }
    /// <summary>
    /// 在“读锁”环境下运行委
    /// <para>1、使用场景：所有action操作都是读，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <returns></returns>
    public static TResult RunInRead<T1, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, TResult> func, in T1 param1)
    {
        ThrowIfNull(func);
        lockSlim.EnterReadLock();
        try { return func.Invoke(param1); }
        finally { lockSlim.ExitReadLock(); }
    }
    /// <summary>
    /// 在“读锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是读，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <param name="param2"><paramref name="func"/>的第二个参数</param>
    /// <returns></returns>
    public static TResult RunInRead<T1, T2, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, T2, TResult> func, in T1 param1, in T2 param2)
    {
        ThrowIfNull(func);
        lockSlim.EnterReadLock();
        try { return func.Invoke(param1, param2); }
        finally { lockSlim.ExitReadLock(); }
    }
    #endregion

    #region 写操作
    /// <summary>
    /// 在“写锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是写；多线程并发读取 </para>
    /// </summary>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    public static void RunInWrite(this ReaderWriterLockSlim lockSlim, in Action action)
    {
        ThrowIfNull(action);
        lockSlim.EnterWriteLock();
        try { action.Invoke(); }
        finally { lockSlim.ExitWriteLock(); }
    }
    /// <summary>
    /// 在“写锁”环境下运行委
    /// <para>1、使用场景：所有action操作都是写；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    public static void RunInWrite<T>(this ReaderWriterLockSlim lockSlim, in Action<T> action, in T param1)
    {
        ThrowIfNull(action);
        lockSlim.EnterWriteLock();
        try { action.Invoke(param1); }
        finally { lockSlim.ExitWriteLock(); }
    }
    /// <summary>
    /// 在“写锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是写；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    /// <param name="param2"><paramref name="action"/>的第二个参数</param>
    public static void RunInWrite<T1, T2>(this ReaderWriterLockSlim lockSlim, in Action<T1, T2> action, in T1 param1, in T2 param2)
    {
        ThrowIfNull(action);
        lockSlim.EnterWriteLock();
        try { action.Invoke(param1, param2); }
        finally { lockSlim.ExitWriteLock(); }
    }

    /// <summary>
    /// 在“写锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是写，但有返回值；多线程并发读取 </para>
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <returns></returns>
    public static TResult RunInWrite<TResult>(this ReaderWriterLockSlim lockSlim, in Func<TResult> func)
    {
        ThrowIfNull(func);
        lockSlim.EnterWriteLock();
        try { return func.Invoke(); }
        finally { lockSlim.ExitWriteLock(); }
    }
    /// <summary>
    /// 在“写锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是写，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <returns></returns>
    public static TResult RunInWrite<T1, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, TResult> func, in T1 param1)
    {
        ThrowIfNull(func);
        lockSlim.EnterWriteLock();
        try { return func.Invoke(param1); }
        finally { lockSlim.ExitWriteLock(); }
    }
    /// <summary>
    /// 在“写锁”环境下运行委托
    /// <para>1、使用场景：所有action操作都是写，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <param name="param2"><paramref name="func"/>的第二个参数</param>
    /// <returns></returns>
    public static TResult RunInWrite<T1, T2, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, T2, TResult> func, in T1 param1, in T2 param2)
    {
        ThrowIfNull(func);
        lockSlim.EnterWriteLock();
        try { return func.Invoke(param1, param2); }
        finally { lockSlim.ExitWriteLock(); }
    }
    #endregion

    #region 升级锁操作
    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁；多线程并发读取 </para>
    /// </summary>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    public static void RunInUpgrade(this ReaderWriterLockSlim lockSlim, in Action action)
    {
        ThrowIfNull(action);
        lockSlim.EnterUpgradeableReadLock();
        try { action.Invoke(); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    public static void RunInUpgrade<T>(this ReaderWriterLockSlim lockSlim, in Action<T> action, in T param1)
    {
        ThrowIfNull(action);
        lockSlim.EnterUpgradeableReadLock();
        try { action.Invoke(param1); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    /// <param name="param2"><paramref name="action"/>的第二个参数</param>
    public static void RunInUpgrade<T1, T2>(this ReaderWriterLockSlim lockSlim, in Action<T1, T2> action, in T1 param1, in T2 param2)
    {
        ThrowIfNull(action);
        lockSlim.EnterUpgradeableReadLock();
        try { action.Invoke(param1, param2); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    /// <summary>
    /// 在“可升级为写锁”环境下运行委
    /// <para>1、使用场景：action操作默认读，可再升级为写锁；多线程并发读取 </para>
    /// <para>2、action可带三个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="action">要执行的动作</param>
    /// <param name="param1"><paramref name="action"/>的第一个参数</param>
    /// <param name="param2"><paramref name="action"/>的第二个参数</param>
    /// <param name="param3"><paramref name="action"/>的第三个参数</param>
    public static void RunInUpgrade<T1, T2, T3>(this ReaderWriterLockSlim lockSlim, in Action<T1, T2, T3> action, in T1 param1, in T2 param2, in T3 param3)
    {
        ThrowIfNull(action);
        lockSlim.EnterUpgradeableReadLock();
        try { action.Invoke(param1, param2, param3); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }

    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁，但有返回值；多线程并发读取 </para>
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <returns></returns>
    public static TResult RunInUpgrade<TResult>(this ReaderWriterLockSlim lockSlim, in Func<TResult> func)
    {
        ThrowIfNull(func);
        lockSlim.EnterUpgradeableReadLock();
        try { return func.Invoke(); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带一个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <returns></returns>
    public static TResult RunInUpgrade<T1, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, TResult> func, in T1 param1)
    {
        ThrowIfNull(func);
        lockSlim.EnterUpgradeableReadLock();
        try { return func.Invoke(param1); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    /// <summary>
    /// 在“可升级为写锁”环境下运行委托
    /// <para>1、使用场景：action操作默认读，可再升级为写锁，但有返回值；多线程并发读取 </para>
    /// <para>2、action可带两个参数，简化方法执行操作，不用专门启一个无参数动作()=>{} </para>
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="lockSlim">读写锁</param>
    /// <param name="func">要执行的动作</param>
    /// <param name="param1"><paramref name="func"/>的第一个参数</param>
    /// <param name="param2"><paramref name="func"/>的第二个参数</param>
    /// <returns></returns>
    public static TResult RunInUpgrade<T1, T2, TResult>(this ReaderWriterLockSlim lockSlim, in Func<T1, T2, TResult> func, in T1 param1, in T2 param2)
    {
        ThrowIfNull(func);
        lockSlim.EnterUpgradeableReadLock();
        try { return func.Invoke(param1, param2); }
        finally { lockSlim.ExitUpgradeableReadLock(); }
    }
    #endregion
}
