namespace Snail.Utilities.Threading.Extensions;
/// <summary>
/// <see cref="AsyncLocal{T}"/>对象扩展
/// </summary>
public static class AsyncLocalExtensions
{
    #region 获取设置值
    /// <summary>
    /// 获取值，不存在则创建
    ///     1、创建时，内部加锁确保只创建一次
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="local"></param>
    /// <param name="setFunc">构建实例的委托</param>
    /// <param name="lockVar">锁变量；值不存在时,加锁构建实例时使用；若传null，则锁local实例自身</param>
    /// <returns></returns>
    public static T GetOrSetValue<T>(this AsyncLocal<T> local, in Func<T> setFunc, in object? lockVar = null)
    {
        //  值为null，则加锁构建
        ThrowIfNull(setFunc);
        if (local.Value == null)
        {
            lock (lockVar ?? local)
            {
                if (local.Value == null)
                {
                    T value = ThrowIfNull(setFunc.Invoke(), "setFunc返回值为null");
                    local.Value = value;
                }
            }
        }
        return local.Value;
    }
    #endregion
}
