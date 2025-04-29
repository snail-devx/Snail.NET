using Snail.Abstractions.Dependency.DataModels;

namespace Snail.Abstractions.Dependency.Extensions
{
    /// <summary>
    /// 依赖注入管理器相关扩展方法
    /// </summary>
    public static class DIManagerExtensions
    {
        #region IsRegistered
        /// <summary>
        /// 判断指定类型是否注册了；key为null
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>已注册返回true；否则返回false</returns>
        public static bool IsRegistered(this IDIManager manager, in Type from)
            => manager.IsRegistered(key: null, from);
        /// <summary>
        /// 判断指定类型是否注册了
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值</param>
        /// <returns>已注册返回true；否则返回false</returns>
        public static bool IsRegistered<T>(this IDIManager manager, in string? key = null)
            => manager.IsRegistered(key, typeof(T));
        #endregion

        #region Register

        #region 非泛型注册
        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为瞬时
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register(this IDIManager manager, in Type from, in Type to)
            => manager.Register(new DIDescriptor(key: null, from, lifetime: LifetimeType.Singleton, to));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register(this IDIManager manager, in Type from, in LifetimeType lifetime, in Type to)
            => manager.Register(new DIDescriptor(key: null, from, lifetime, to));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register(this IDIManager manager, in string? key, in Type from, in Type to)
            => manager.Register(new DIDescriptor(key, from, lifetime: LifetimeType.Singleton, to));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register(this IDIManager manager, in string? key, in Type from, in LifetimeType lifetime, in Type to)
            => manager.Register(new DIDescriptor(key, from, lifetime, to));
        #endregion

        #region 泛型注册
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<T>(this IDIManager manager, in string? key = null, in LifetimeType lifetime = LifetimeType.Singleton)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime));

        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<T>(this IDIManager manager, in Func<IDIManager, T> toFunc) where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime: LifetimeType.Singleton, toFunc));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<T>(this IDIManager manager, in LifetimeType lifetime, in Func<IDIManager, T> toFunc) where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime, toFunc));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<T>(this IDIManager manager, in string? key, in Func<IDIManager, T> toFunc) where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime: LifetimeType.Singleton, toFunc));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<T>(this IDIManager manager, in string? key, in LifetimeType lifetime, in Func<IDIManager, T> toFunc)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime, toFunc));

        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public static IDIManager Register<T>(this IDIManager manager, T instance) where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime: LifetimeType.Singleton, instance));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public static IDIManager Register<T>(this IDIManager manager, in LifetimeType lifetime, T instance) where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime, instance));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public static IDIManager Register<T>(this IDIManager manager, in string? key, T instance) where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime: LifetimeType.Singleton, instance));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public static IDIManager Register<T>(this IDIManager manager, in string? key, in LifetimeType lifetime, T instance) where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime, instance));

        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="FromType">依赖注入源类型</typeparam>
        /// <typeparam name="ToType">依赖注入实现类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public static IDIManager Register<FromType, ToType>(this IDIManager manager, in string? key = null, in LifetimeType lifetime = LifetimeType.Singleton)
            where ToType : class, FromType
            => manager.Register(new DIDescriptor<FromType, ToType>(key, lifetime));
        #endregion

        #endregion

        #region Unregister
        /// <summary>
        /// 反注册符合条件的依赖注入信息：key为null
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>返回自身，方便链式调用</returns>
        public static IDIManager Unregister(this IDIManager manager, in Type from)
            => manager.Unregister(key: null, from);

        /// <summary>
        /// 反注册符合条件的依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="manager"></param>
        /// <param name="key">依赖注入Key值</param>
        /// <returns></returns>
        public static IDIManager Unregister<T>(this IDIManager manager, in string? key = null)
            => manager.Unregister(key, typeof(T));
        #endregion

        #region Resolve
        /// <summary>
        /// 构建实例；key为null
        /// </summary>
        /// <param name="manager">依赖注入管理器实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>构建成功的实例；否则返回null</returns>
        public static object? Resolve(this IDIManager manager, in Type from)
            => manager.Resolve(key: null, from);
        /// <summary>
        /// 构建泛型实例
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="manager">依赖注入管理器实例</param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns>构建成功的实例；否则返回null</returns>
        public static T? Resolve<T>(this IDIManager manager, in string? key = null)
            => (T?)manager.Resolve(key, typeof(T));

        /// <summary>
        /// 构建有效实例，返回null报错；key为null
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static object ResolveRequired(this IDIManager manager, in Type from)
            => ResolveRequired(manager, key: null, from);
        /// <summary>
        /// 构建有效实例，返回null报错
        /// </summary>
        /// <param name="manager">依赖注入管理器实例</param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static object ResolveRequired(this IDIManager manager, in string? key, in Type from)
        {
            object? value = manager.Resolve(key, from);
            ThrowIfNull(value, $"构建实例失败：Resolve返回null。key:{key ?? STR_Null};from:{from.FullName}");
            return value!;
        }
        /// <summary>
        /// 构建有效泛型实例，返回null报错
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="manager">依赖注入管理器实例</param>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static T ResolveRequired<T>(this IDIManager manager, in string? key = null)
        {
            object value = ResolveRequired(manager, key, typeof(T));
            return (T)value;
        }
        #endregion
    }
}
