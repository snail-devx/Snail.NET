namespace Snail.Utilities.Threading.Extensions;
/// <summary>
/// <see cref="SemaphoreSlim"/>扩展方法，用于异步方法中做并发控制
/// </summary>
public static class SemaphoreSlimExtensions
{
    #region 公共方法
    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="slim"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static async Task<RunResult> Run(this SemaphoreSlim slim, Action action)
    {
        ThrowIfNull(action);
        try
        {
            await slim.WaitAsync();
            action.Invoke();
            return true;
        }
        finally
        {
            slim.Release();
        }
    }

    #endregion
}
