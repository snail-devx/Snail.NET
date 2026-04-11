namespace Snail.Utilities.Common.Utils;
/// <summary>
/// 委托助手类
/// <para>1、支持运行Action和Func：Run、RunIf、RunAsync、RunIfAsync </para>
/// </summary>
public static class DelegateHelper
{
    #region 公共方法

    #region Run：Action；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <param name="action">要运行的委托</param>
    /// <returns>运行结果对象</returns>
    public static RunResult Run(Action action)
    {
        try
        {
            ThrowIfNull(action);
            action.Invoke();
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <param name="action">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象</returns>
    public static RunResult Run<T1>(Action<T1> action, T1 param1)
    {
        try
        {
            ThrowIfNull(action);
            action.Invoke(param1);
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="action">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象</returns>
    public static RunResult Run<T1, T2>(Action<T1, T2> action, T1 param1, T2 param2)
    {
        try
        {
            ThrowIfNull(action);
            action.Invoke(param1, param2);
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="action">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数2</param>
    /// <returns>运行结果对象</returns>
    public static RunResult Run<T1, T2, T3>(Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
    {
        try
        {
            ThrowIfNull(action);
            action.Invoke(param1, param2, param3);
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <param name="action">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数2</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象</returns>
    public static RunResult Run<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        try
        {
            ThrowIfNull(action);
            action.Invoke(param1, param2, param3, param4);
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    #endregion

    #region RunIf：Action；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <param name="condition">断言条件</param>
    /// <param name="action">要执行的委托</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult RunIf(bool condition, Action action)
        => condition == true ? Run(action) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <param name="condition">断言条件</param>
    /// <param name="action">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult RunIf<T1>(bool condition, Action<T1> action, T1 param1)
        => condition == true ? Run(action, param1) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="condition">断言条件</param>
    /// <param name="action">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult RunIf<T1, T2>(bool condition, Action<T1, T2> action, T1 param1, T2 param2)
        => condition == true ? Run(action, param1, param2) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="condition">断言条件</param>
    /// <param name="action">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult RunIf<T1, T2, T3>(bool condition, Action<T1, T2, T3> action, T1 param1, T2 param2, T3 param3)
        => condition == true ? Run(action, param1, param2, param3) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <param name="condition">断言条件</param>
    /// <param name="action">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult RunIf<T1, T2, T3, T4>(bool condition, Action<T1, T2, T3, T4> action, T1 param1, T2 param2, T3 param3, T4 param4)
        => condition == true ? Run(action, param1, param2, param3, param4) : RunResult.FAILED;
    #endregion

    #region Run：Func；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <returns>运行结果对象</returns>
    public static RunResult<R> Run<R>(Func<R> func)
    {
        try
        {
            ThrowIfNull(func);
            return func.Invoke();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象</returns>
    public static RunResult<R> Run<T1, R>(Func<T1, R> func, T1 param1)
    {
        try
        {
            ThrowIfNull(func);
            return func.Invoke(param1);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象</returns>
    public static RunResult<R> Run<T1, T2, R>(Func<T1, T2, R> func, T1 param1, T2 param2)
    {
        try
        {
            ThrowIfNull(func);
            return func.Invoke(param1, param2);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象</returns>
    public static RunResult<R> Run<T1, T2, T3, R>(Func<T1, T2, T3, R> func, T1 param1, T2 param2, T3 param3)
    {
        try
        {
            ThrowIfNull(func);
            return func.Invoke(param1, param2, param3);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象</returns>
    public static RunResult<R> Run<T1, T2, T3, T4, R>(Func<T1, T2, T3, T4, R> func, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        try
        {
            ThrowIfNull(func);
            return func.Invoke(param1, param2, param3, param4);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    #endregion

    #region RunIf：Func；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult<R> RunIf<R>(bool condition, Func<R> func)
        => condition == true ? Run(func) : RunResult<R>.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult<R> RunIf<T1, R>(bool condition, Func<T1, R> func, T1 param1)
        => condition == true ? Run(func, param1) : RunResult<R>.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult<R> RunIf<T1, T2, R>(bool condition, Func<T1, T2, R> func, T1 param1, T2 param2)
        => condition == true ? Run(func, param1, param2) : RunResult<R>.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult<R> RunIf<T1, T2, T3, R>(bool condition, Func<T1, T2, T3, R> func, T1 param1, T2 param2, T3 param3)
        => condition == true ? Run(func, param1, param2, param3) : RunResult<R>.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static RunResult<R> RunIf<T1, T2, T3, T4, R>(bool condition, Func<T1, T2, T3, T4, R> func, T1 param1, T2 param2, T3 param3, T4 param4)
        => condition == true ? Run(func, param1, param2, param3, param4) : RunResult<R>.FAILED;
    #endregion

    #region RunAsync：Func-Async；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <param name="func">要运行的委托</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult> RunAsync(Func<Task> func)
    {
        try
        {
            ThrowIfNull(func);
            await func.Invoke();
            return RunResult.SUCCESS;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult<R>> RunAsync<R>(Func<Task<R>> func)
    {
        try
        {
            ThrowIfNull(func);
            R r = await func.Invoke();
            return r;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult> RunAsync<T1>(Func<T1, Task> func, T1 param1)
    {
        try
        {
            ThrowIfNull(func);
            await func.Invoke(param1);
            return true;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult<R>> RunAsync<T1, R>(Func<T1, Task<R>> func, T1 param1)
    {
        try
        {
            ThrowIfNull(func);
            R r = await func.Invoke(param1);
            return r;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult> RunAsync<T1, T2>(Func<T1, T2, Task> func, T1 param1, T2 param2)
    {
        try
        {
            ThrowIfNull(func);
            await func.Invoke(param1, param2);
            return true;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult<R>> RunAsync<T1, T2, R>(Func<T1, T2, Task<R>> func, T1 param1, T2 param2)
    {
        try
        {
            ThrowIfNull(func);
            R r = await func.Invoke(param1, param2);
            return r;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult> RunAsync<T1, T2, T3>(Func<T1, T2, T3, Task> func, T1 param1, T2 param2, T3 param3)
    {
        try
        {
            ThrowIfNull(func);
            await func.Invoke(param1, param2, param3);
            return true;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult<R>> RunAsync<T1, T2, T3, R>(Func<T1, T2, T3, Task<R>> func, T1 param1, T2 param2, T3 param3)
    {
        try
        {
            ThrowIfNull(func);
            R r = await func.Invoke(param1, param2, param3);
            return r;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult> RunAsync<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> func, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        try
        {
            ThrowIfNull(func);
            await func.Invoke(param1, param2, param3, param4);
            return true;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    /// <summary>
    /// 运行委托，捕捉异常
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数4类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="func">要运行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象</returns>
    public static async Task<RunResult<R>> RunAsync<T1, T2, T3, T4, R>(Func<T1, T2, T3, T4, Task<R>> func, T1 param1, T2 param2, T3 param3, T4 param4)
    {
        try
        {
            ThrowIfNull(func);
            R r = await func.Invoke(param1, param2, param3, param4);
            return r;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    #endregion

    #region RunIfAsync：Func-Async；最多支持4个参数，多个意义不大，外部应该做简化
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult> RunIfAsync(bool condition, Func<Task> func)
        => condition == true ? await RunAsync(func) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult<R>> RunIfAsync<R>(bool condition, Func<Task<R>> func)
        => condition == true ? await RunAsync(func) : RunResult<R>.FAILED;

    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult> RunIfAsync<T1>(bool condition, Func<T1, Task> func, T1 param1)
        => condition == true ? await RunAsync(func, param1) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult<R>> RunIfAsync<T1, R>(bool condition, Func<T1, Task<R>> func, T1 param1)
        => condition == true ? await RunAsync(func, param1) : RunResult<R>.FAILED;

    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult> RunIfAsync<T1, T2>(bool condition, Func<T1, T2, Task> func, T1 param1, T2 param2)
            => condition == true ? await RunAsync(func, param1, param2) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult<R>> RunIfAsync<T1, T2, R>(bool condition, Func<T1, T2, Task<R>> func, T1 param1, T2 param2)
        => condition == true ? await RunAsync(func, param1, param2) : RunResult<R>.FAILED;

    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult> RunIfAsync<T1, T2, T3>(bool condition, Func<T1, T2, T3, Task> func, T1 param1, T2 param2, T3 param3)
        => condition == true ? await RunAsync(func, param1, param2, param3) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult<R>> RunIfAsync<T1, T2, T3, R>(bool condition, Func<T1, T2, T3, Task<R>> func, T1 param1, T2 param2, T3 param3)
        => condition == true ? await RunAsync(func, param1, param2, param3) : RunResult<R>.FAILED;

    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数3类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult> RunIfAsync<T1, T2, T3, T4>(bool condition, Func<T1, T2, T3, T4, Task> func, T1 param1, T2 param2, T3 param3, T4 param4)
        => condition == true ? await RunAsync(func, param1, param2, param3, param4) : RunResult.FAILED;
    /// <summary>
    /// 运行委托，捕捉异常：<paramref name="condition"/> 为true，才运行
    /// </summary>
    /// <typeparam name="T1">参数1类型</typeparam>
    /// <typeparam name="T2">参数2类型</typeparam>
    /// <typeparam name="T3">参数3类型</typeparam>
    /// <typeparam name="T4">参数3类型</typeparam>
    /// <typeparam name="R">返回值类型</typeparam>
    /// <param name="condition">判断条件</param>
    /// <param name="func">要执行的委托</param>
    /// <param name="param1">委托参数1</param>
    /// <param name="param2">委托参数2</param>
    /// <param name="param3">委托参数3</param>
    /// <param name="param4">委托参数4</param>
    /// <returns>运行结果对象；若不满足<paramref name="condition"/>返回结果对象为false，但无异常对象</returns>
    public static async Task<RunResult<R>> RunIfAsync<T1, T2, T3, T4, R>(bool condition, Func<T1, T2, T3, T4, Task<R>> func, T1 param1, T2 param2, T3 param3, T4 param4)
        => condition == true ? await RunAsync(func, param1, param2, param3, param4) : RunResult<R>.FAILED;
    #endregion

    #endregion
}
