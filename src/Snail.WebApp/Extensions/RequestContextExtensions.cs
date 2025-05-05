namespace Snail.WebApp.Extensions
{
    /// <summary>
    /// 网络请求上下文扩展法昂发 <br />
    ///     1、ActionContext及其派生类ActionExecutingContext、ActionExecutedContext <br />
    ///     2、HttpContext<br />
    /// </summary>
    public static class RequestContextExtensions
    {
        #region ActionContext
        /// <summary>
        /// 从动作上下文分析特性标签
        ///     1、获取顺序：Action->Controller->ServiceProvider->new
        ///     2、内部会自动缓存已去过的特性标签，方便后续重复获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="controller">context所属控制器实例；可为null</param>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(this ActionContext context, Object? controller) where T : Attribute, new()
        {
            ActionDescriptor action = context.ActionDescriptor;
            ThrowIfNull(action);
            //  查一下是否有缓存中，若缓存了，则不用再取了
            //      有异步并发问题，先注释不缓存
            //Type type = typeof(T);
            //action.Properties.TryGetValue(type, out Object? tmpValue);
            //if (tmpValue != null) return (T)tmpValue;
            //  判断Action上是否有此特性标签
            T? attr = (action as ControllerActionDescriptor)?.MethodInfo?.GetCustomAttribute<T>()
                ?? controller?.GetType()?.GetCustomAttribute<T>()
                ?? context.HttpContext?.RequestServices?.GetService<T>()
                ?? new T();
            //      有异步并发问题，先注释不缓存
            //action.Properties[type] = attr;

            return attr;
        }
        #endregion

        #region HttpContext
        /// <summary>
        /// 为当前网络上下文设置数据，方便网络请求操作中共享。
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetData(this HttpContext context, string key, object? value)
        {
            ThrowIfNull(key);
            context.Items[key] = value;
        }
        /// <summary>
        /// 获取上下文设置的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetData<T>(this HttpContext context, string key)
        {
            ThrowIfNull(key);
            context.Items.TryGetValue(key, out object? value);
            return value == null ? default! : (T)value;
        }
        #endregion
    }
}
