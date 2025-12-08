namespace Snail.Utilities.Common.Extensions;
/// <summary>
/// <see cref="Exception"/>扩展方法
/// </summary>
public static class ExceptionExtensions
{
    #region 公共方法
    /// <summary>
    /// 对异常对象做优化
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public static Exception Optimize(this Exception ex)
    {
        switch (ex)
        {
            //  针对AggregateException做优化处理，如果只有一个内部异常，则返回内部异常自身，避免太繁琐
            case AggregateException ae:
                ex = ae.InnerExceptions.Count == 1 ? ae.InnerExceptions[0] : ae;
                return ex.Optimize();
            //  其他情况，先返回自身
            default: return ex;
        }
    }
    #endregion
}
