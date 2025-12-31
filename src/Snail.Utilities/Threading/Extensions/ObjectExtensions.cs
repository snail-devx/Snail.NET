namespace Snail.Utilities.Threading.Extensions;

/// <summary>
/// <see cref="object"/>针对多线程的扩展
/// </summary>
public static class ObjectExtensions
{
    #region 扩展方法

    #region 加锁执行Func
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <typeparam name="T">加锁后执行<paramref name="lockFunc"/>的返回值</typeparam>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockFunc">加锁成功后执行的Func</param>
    /// <returns></returns>
    public static T? TryLock<T>(this object obj, Func<bool> predicate, Func<T> lockFunc)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    return lockFunc();
                }
            }
        }
        return default;
    }
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <typeparam name="P1">加锁后执行<paramref name="lockFunc"/>的参数1类型</typeparam>
    /// <typeparam name="T">加锁后执行<paramref name="lockFunc"/>的返回值</typeparam>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockFunc">加锁成功后执行的Func</param>
    /// <param name="p1">加锁成功后执行<paramref name="lockFunc"/>，传递的参数<paramref name="p1"/></param>
    /// <returns></returns>
    public static T? TryLock<P1, T>(this object obj, Func<bool> predicate, Func<P1, T> lockFunc, P1 p1)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    return lockFunc(p1);
                }
            }
        }
        return default;
    }
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <typeparam name="P1">加锁后执行<paramref name="lockFunc"/>的参数1类型</typeparam>
    /// <typeparam name="P2">加锁后执行<paramref name="lockFunc"/>的参数2类型</typeparam>
    /// <typeparam name="T">加锁后执行<paramref name="lockFunc"/>的返回值</typeparam>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockFunc">加锁成功后执行的Func</param>
    /// <param name="p1">加锁成功后执行<paramref name="lockFunc"/>，传递的参数<paramref name="p1"/></param>
    /// <param name="p2">加锁成功后执行<paramref name="lockFunc"/>，传递的参数<paramref name="p1"/></param>
    /// <returns></returns>
    public static T? TryLock<P1, P2, T>(this object obj, Func<bool> predicate, Func<P1, P2, T> lockFunc, P1 p1, P2 p2)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    return lockFunc(p1, p2);
                }
            }
        }
        return default;
    }
    #endregion

    #region 加锁执行Action
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockAction">加锁成功后执行的Func</param>
    /// <returns></returns>
    public static void TryLock(this object obj, Func<bool> predicate, Action lockAction)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    lockAction();
                }
            }
        }
    }
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <typeparam name="P1">加锁后执行<paramref name="lockAction"/>的参数1类型</typeparam>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockAction">加锁成功后执行的Func</param>
    /// <param name="p1">加锁成功后执行<paramref name="lockAction"/>，传递的参数<paramref name="p1"/></param>
    /// <returns></returns>
    public static void TryLock<P1>(this object obj, Func<bool> predicate, Action<P1> lockAction, P1 p1)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    lockAction(p1);
                }
            }
        }
    }
    /// <summary>
    /// 断言条件为true时，对obj进行加锁
    /// </summary>
    /// <typeparam name="P1">加锁后执行<paramref name="lockAction"/>的参数1类型</typeparam>
    /// <typeparam name="P2">加锁后执行<paramref name="lockAction"/>的参数2类型</typeparam>
    /// <param name="obj">要加锁的对象</param>
    /// <param name="predicate">加锁断言条件，满足时才加锁</param>
    /// <param name="lockAction">加锁成功后执行的Func</param>
    /// <param name="p1">加锁成功后执行<paramref name="lockAction"/>，传递的参数<paramref name="p1"/></param>
    /// <param name="p2">加锁成功后执行<paramref name="lockAction"/>，传递的参数<paramref name="p1"/></param>
    /// <returns></returns>
    public static void TryLock<P1, P2>(this object obj, Func<bool> predicate, Action<P1, P2> lockAction, P1 p1, P2 p2)
    {
        if (predicate() == true)
        {
            lock (obj)
            {
                if (predicate() == true)
                {
                    lockAction(p1, p2);
                }
            }
        }
    }
    #endregion

    #endregion
}
