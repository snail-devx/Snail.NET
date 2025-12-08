namespace Snail.Utilities.Threading.Extensions;
/// <summary>
/// <see cref="Task"/>扩展方法
/// </summary>
public static class TaskExtensions
{
    #region 公共方法
    /// <summary>
    /// 运行任务，得到运行结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task">要运行的任务：内部会自动执行Start方法</param>
    /// <returns></returns>
    public static T Run<T>(this Task<T> task)
    {
        ThrowIfNull(task);
        // 后期根据Status进行是否需要先Start；但怕外部没Start或者再Start
        task.Start();
        task.Wait();
        return task.Result;
    }

    /// <summary>
    /// 等待任务运行结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="task">要等待的任务；正在运行的，内部不会执行Start方法</param>
    /// <returns></returns>
    public static T WaitResult<T>(this Task<T> task)
    {
        // 后期根据Status进行判断是否需要先Start；但怕外部没Start或者再Start
        ThrowIfNull(task);
        task.Wait();
        return task.Result;
    }
    #endregion
}
