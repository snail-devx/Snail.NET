using Snail.Utilities.Common.Utils;

namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// 值类型 扩展方法：如bool、int、、、
/// </summary>
public static class ValueTypeExtensions
{
    #region boolean 扩展
    /// <summary>
    /// value为true时，执行委托
    /// <para>1、方便链式调用使用</para>
    /// <para>2、本类扩展方法不会，仅关注调用，发生异常会抛出；而<see cref="DelegateHelper"/>还关注执行结果，会拦截异常，确保外部无异常</para>
    /// </summary>
    /// <param name="value"></param>
    extension(bool value)
    {
        #region Run-Action；最多支持4个参数，多个意义不大，外部应该做简化
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="action"/>
        /// </summary>
        /// <param name="action">要执行的委托</param>
        public void Run(Action action)
        {
            if (value == true)
            {
                ThrowIfNull(action).Invoke();
            }
        }
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="action"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <param name="action">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        public void Run<T1>(Action<T1> action, T1 param1)
        {
            if (value == true)
            {
                ThrowIfNull(action).Invoke(param1);
            }
        }
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="action"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="action">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        public void Run<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
        {
            if (value == true)
            {
                ThrowIfNull(action).Invoke(param1, param2);
            }
        }
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="action"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="action">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        public void Run<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        {
            if (value == true)
            {
                ThrowIfNull(action).Invoke(param1, param2, param3);
            }
        }
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="action"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="T4">参数4类型</typeparam>
        /// <param name="action">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <param name="param4">委托参数4</param>
        public void Run<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (value == true)
            {
                ThrowIfNull(action).Invoke(param1, param2, param3, param4);
            }
        }
        #endregion

        #region Run-Func；最多支持4个参数，多个意义不大，外部应该做简化
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public R? Run<R>(Func<R> func)
            => value == true ? ThrowIfNull(func).Invoke() : default;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public R? Run<T1, R>(Func<T1, R> func, T1 param1)
            => value == true ? ThrowIfNull(func).Invoke(param1) : default;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public R? Run<T1, T2, R>(Func<T1, T2, R> func, T1 param1, T2 param2)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2) : default;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public R? Run<T1, T2, T3, R>(Func<T1, T2, T3, R> func, T1 param1, T2 param2, T3 param3)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3) : default;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="T4">参数4类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <param name="param4">委托参数4</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public R? Run<T1, T2, T3, T4, R>(Func<T1, T2, T3, T4, R> func, T1 param1, T2 param2, T3 param3, T4 param4)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3, param4) : default;
        #endregion

        #region Run-Func-Async；最多支持4个参数，多个意义不大，外部应该做简化。实际上可以使用Run-Func完成，这里为了语义简单，独立出来
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <param name="func">要执行的委托</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task? RunAsync(Func<Task> func)
            => value == true ? ThrowIfNull(func).Invoke() : null;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task<R>? RunAsync<R>(Func<Task<R>> func)
            => value == true ? ThrowIfNull(func).Invoke() : null;

        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task? RunAsync<T1>(Func<T1, Task> func, T1 param1)
            => value == true ? ThrowIfNull(func).Invoke(param1) : null;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task<R>? RunAsync<T1, R>(Func<T1, Task<R>> func, T1 param1)
            => value == true ? ThrowIfNull(func).Invoke(param1) : null;

        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task? RunAsync<T1, T2>(Func<T1, T2, Task> func, T1 param1, T2 param2)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2) : null;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task<R>? RunAsync<T1, T2, R>(Func<T1, T2, Task<R>> func, T1 param1, T2 param2)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2) : null;

        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task? RunAsync<T1, T2, T3>(Func<T1, T2, T3, Task> func, T1 param1, T2 param2, T3 param3)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3) : null;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task<R>? RunAsync<T1, T2, T3, R>(Func<T1, T2, T3, Task<R>> func, T1 param1, T2 param2, T3 param3)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3) : null;

        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="T4">参数4类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <param name="param4">委托参数4</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task? RunAsync<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> func, T1 param1, T2 param2, T3 param3, T4 param4)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3, param4) : null;
        /// <summary>
        /// <paramref name="value"/>为ture时，执行<paramref name="func"/>
        /// </summary>
        /// <typeparam name="T1">参数1类型</typeparam>
        /// <typeparam name="T2">参数2类型</typeparam>
        /// <typeparam name="T3">参数3类型</typeparam>
        /// <typeparam name="T4">参数4类型</typeparam>
        /// <typeparam name="R">返回值类型</typeparam>
        /// <param name="func">要执行的委托</param>
        /// <param name="param1">委托参数1</param>
        /// <param name="param2">委托参数2</param>
        /// <param name="param3">委托参数3</param>
        /// <param name="param4">委托参数4</param>
        /// <returns>执行结果；<paramref name="value"/>为false时，default值</returns>
        public Task<R>? RunAsync<T1, T2, T3, T4, R>(Func<T1, T2, T3, T4, Task<R>> func, T1 param1, T2 param2, T3 param3, T4 param4)
            => value == true ? ThrowIfNull(func).Invoke(param1, param2, param3, param4) : null;
        #endregion
    }
    #endregion
}