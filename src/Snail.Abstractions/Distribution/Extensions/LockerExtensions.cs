using Snail.Abstractions.Distribution.Exceptions;
using Snail.Utilities.Common.Utils;
using Snail.Utilities.Threading.Extensions;

namespace Snail.Abstractions.Distribution.Extensions;

/// <summary>
/// <see cref="ILocker"/>扩展方法
/// </summary>
public static class LockerExtensions
{
    #region 公共方法
    /// <summary>
    /// 尝试加锁；拦截加锁异常
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁的Key；确保唯一</param>
    /// <param name="value">锁的值；在释放锁时使用；只有值正确才能被释放掉</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；为null默认20次；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥锁</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult> TryLock(this ILocker locker, string key, string value, uint maxTryCount = 20, int expireSeconds = 60)
    {
        RunResult<bool> lockResult = await DelegateHelper.RunAsync(locker.Lock, key, value, maxTryCount, expireSeconds);
        if (lockResult.Success == false)
        {
            Exception innerEx = lockResult.Exception ?? new Exception("超过最大尝试次数，仍未加锁成功");
            return new LockException(key, value, innerEx);
        }
        return RunResult.SUCCESS;
    }
    /// <summary>
    /// 尝试解锁；拦截解锁异常
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁的Key</param>
    /// <param name="value">锁的值；加锁时传入的锁值</param>
    /// <returns>运行结果；若为解锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult> TryUnlock(this ILocker locker, string key, string value)
    {
        //  解锁失败，但无异常信息，则构建LockException异常返回
        RunResult<bool> rt = await DelegateHelper.RunAsync(locker.Unlock, key, value);
        if (rt.Data == false && rt.Exception == null)
        {
            return new LockException(key, value);
        }
        return rt;
    }

    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="action">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult> Run(this ILocker locker, string key, string value, Action action, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(action);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult runResult = await locker.TryLock(key, value, maxTryCount);
        if (runResult.Success == true)
        {
            runResult = DelegateHelper.Run(action);
            await locker.TryUnlock(key, value);
        }
        return runResult;
    }
    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="func">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult> Run(this ILocker locker, string key, string value, Func<Task> func, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(func);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult runResult = await locker.TryLock(key, value, maxTryCount);
        if (runResult.Success == true)
        {
            runResult = await DelegateHelper.RunAsync(func);
            await locker.TryUnlock(key, value);
        }
        return runResult;
    }
    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="func">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult<T>> Run<T>(this ILocker locker, string key, string value, Func<T> func, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(func);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult lt = await locker.TryLock(key, value, maxTryCount);
        if (lt.Success == true)
        {
            RunResult<T> rt = DelegateHelper.Run(func);
            await locker.TryUnlock(key, value);
            return rt;
        }
        return lt.Exception!;
    }
    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="func">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static async Task<RunResult<T>> Run<T>(this ILocker locker, string key, string value, Func<Task<T>> func, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(func);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult lt = await locker.TryLock(key, value, maxTryCount);
        if (lt.Success == true)
        {
            RunResult<T> rt = await DelegateHelper.RunAsync(func);
            await locker.TryUnlock(key, value);
            return rt;
        }
        return lt.Exception!;
    }

    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="action">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static RunResult RunSync(this ILocker locker, string key, string value, Action action, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(action);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult runResult = locker.TryLock(key, value, maxTryCount).WaitResult();
        if (runResult.Success == true)
        {
            runResult = DelegateHelper.Run(action);
            locker.TryUnlock(key, value).Wait();
        }
        return runResult;
    }
    /// <summary>
    /// 加锁运行委托
    /// </summary>
    /// <param name="locker">加锁器</param>
    /// <param name="key">加锁Key</param>
    /// <param name="value">加锁值</param>
    /// <param name="func">执行委托</param>
    /// <param name="maxTryCount">本次加锁尝试失败的最大重试次数；每次重试间隔100ms。最大重试400次；为0则表示不尝试等待加锁，互斥所</param>
    /// <param name="expireSeconds">锁的过期时间（单位秒），防止死锁；&lt;=0 则默认10分钟</param>
    /// <returns>运行结果；若为加锁失败，则<see cref="RunResult.Exception"/>为<see cref="LockException"/></returns>
    public static RunResult<T> RunSync<T>(this ILocker locker, string key, string value, Func<T> func, uint maxTryCount = 20, int expireSeconds = 60)
    {
        ThrowIfNull(func);
        //  加锁成功后运行委托，只要加锁成功必须尝试解锁
        RunResult lt = locker.TryLock(key, value, maxTryCount).WaitResult();
        if (lt.Success == true)
        {
            RunResult<T> rt = DelegateHelper.Run(func);
            locker.TryUnlock(key, value).Wait();
            return rt;
        }
        return lt.Exception!;
    }
    #endregion
}
