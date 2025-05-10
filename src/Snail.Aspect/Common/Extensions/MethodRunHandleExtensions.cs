using System;
using System.Threading.Tasks;
using Snail.Aspect.Common.Components;
using Snail.Aspect.Common.Interfaces;

namespace Snail.Aspect.Common.Extensions
{
    /// <summary>
    /// <see cref="IMethodRunHandle"/>扩展方法
    /// </summary>
    public static class MethodRunHandleExtensions
    {
        #region 公共方法
        /// <summary>
        /// 异步方法运行时；支持返回返回值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <param name="next"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task<T> OnRunAsync<T>(this IMethodRunHandle handle, Func<Task<T>> next, MethodRunContext context)
        {
            await handle.OnRunAsync(async () =>
            {
                T data = await next.Invoke();
                context.SetReturnValue(data);
            }, context);
            return (T)context.ReturnValue;
        }

        /// <summary>
        /// 同步方法运行时；支持返回返回值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <param name="next"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static T OnRun<T>(this IMethodRunHandle handle, Func<T> next, MethodRunContext context)
        {
            handle.OnRun(() =>
            {
                T data = next.Invoke();
                context.SetReturnValue(data);
            }, context);
            return (T)context.ReturnValue;
        }
        #endregion
    }
}
