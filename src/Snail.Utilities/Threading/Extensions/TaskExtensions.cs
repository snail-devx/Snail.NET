namespace Snail.Utilities.Threading.Extensions;
/// <summary>
/// <see cref="Task"/>扩展方法
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    /// <see cref="Task"/>扩展方法
    /// </summary>
    extension<T>(Task<T> task)
    {
        /// <summary>
        /// 运行任务，得到运行结果
        /// </summary>
        /// <returns></returns>
        public T Run()
        {
            // 后期根据Status进行是否需要先Start；但怕外部没Start或者再Start
            task.Start();
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// 等待任务运行结果
        /// </summary>
        /// <returns></returns>
        public T WaitResult()
        {
            // 后期根据Status进行判断是否需要先Start；但怕外部没Start或者再Start
            task.Wait();
            return task.Result;
        }
    }
}
