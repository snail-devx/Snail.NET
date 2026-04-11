using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;
using static Snail.Abstractions.Dependency.Enumerations.LifetimeType;

namespace Snail.Abstractions.Dependency.Extensions;

/// <summary>
/// 依赖注入管理器相关扩展方法
/// </summary>
public static class DIManagerExtensions
{
    extension(IDIManager manager)
    {
        #region IsRegistered
        /// <summary>
        /// 判断指定类型是否注册了
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值</param>
        /// <returns>已注册返回true；否则返回false</returns>
        public bool IsRegistered<T>(string? key = null)
            => manager.IsRegistered(key, typeof(T));
        /// <summary>
        /// 判断指定类型是否注册了；key为null
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>已注册返回true；否则返回false</returns>
        public bool IsRegistered(Type from)
            => manager.IsRegistered(key: null, from);
        #endregion

        #region Register

        #region 泛型注册
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<T>(string? key = null, LifetimeType lifetime = Singleton)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime));

        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<T>(Func<IDIManager, T> toFunc)
            where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime: Singleton, toFunc));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="lifetime">生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<T>(LifetimeType lifetime, Func<IDIManager, T> toFunc)
            where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime, toFunc));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<T>(string? key, Func<IDIManager, T> toFunc)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime: Singleton, toFunc));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="toFunc">构建to实例的委托方法</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<T>(string? key, LifetimeType lifetime, Func<IDIManager, T> toFunc)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime, toFunc));

        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public IDIManager Register<T>(T instance) where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime: Singleton, instance));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="lifetime">生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public IDIManager Register<T>(LifetimeType lifetime, T instance)
            where T : class
            => manager.Register(new DIDescriptor<T>(key: null, lifetime, instance));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public IDIManager Register<T>(string? key, T instance)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime: Singleton, instance));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源、目标类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="instance">对象实例；内部转换成 Func委托：manager=>instance</param>
        /// <returns></returns>
        public IDIManager Register<T>(string? key, LifetimeType lifetime, T instance)
            where T : class
            => manager.Register(new DIDescriptor<T>(key, lifetime, instance));

        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <typeparam name="FromType">依赖注入源类型</typeparam>
        /// <typeparam name="ToType">依赖注入实现类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register<FromType, ToType>(string? key = null, LifetimeType lifetime = Singleton)
            where ToType : class, FromType
            => manager.Register(new DIDescriptor<FromType, ToType>(key, lifetime));
        #endregion

        #region 非泛型注册
        /// <summary>
        /// 注册依赖注入信息；key为null，lifetime为瞬时
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register(Type from, Type to)
            => manager.Register(new DIDescriptor(key: null, from, lifetime: Singleton, to));
        /// <summary>
        /// 注册依赖注入信息；key为null
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register(Type from, LifetimeType lifetime, Type to)
            => manager.Register(new DIDescriptor(key: null, from, lifetime, to));
        /// <summary>
        /// 注册依赖注入信息；lifetime为单例
        /// </summary>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register(string? key, Type from, Type to)
            => manager.Register(new DIDescriptor(key, from, lifetime: Singleton, to));
        /// <summary>
        /// 注册依赖注入信息
        /// </summary>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="lifetime">生命周期</param>
        /// <param name="to">依赖注入实现类型</param>
        /// <returns>依赖注入管理器，方便链式调用</returns>
        public IDIManager Register(string? key, Type from, LifetimeType lifetime, Type to)
            => manager.Register(new DIDescriptor(key, from, lifetime, to));
        #endregion

        #endregion

        #region Unregister
        /// <summary>
        /// 反注册符合条件的依赖注入信息
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值</param>
        /// <returns></returns>
        public IDIManager Unregister<T>(string? key = null)
            => manager.Unregister(key, typeof(T));
        /// <summary>
        /// 反注册符合条件的依赖注入信息：key为null
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <returns>返回自身，方便链式调用</returns>
        public IDIManager Unregister(Type from)
            => manager.Unregister(key: null, from);
        #endregion

        #region Resolve
        /// <summary>
        /// 构建实例；key为null
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
        /// <returns>构建成功的实例；否则返回null</returns>
        public object? Resolve(Type from, IParameter[]? parameters = null)
            => manager.Resolve(key: null, from, parameters);
        /// <summary>
        /// 构建有效实例，返回null报错；key为null
        /// </summary>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public object ResolveRequired(Type from, IParameter[]? parameters = null)
            => manager.ResolveRequired(key: null, from, parameters);
        /// <summary>
        /// 构建有效实例，返回null报错
        /// </summary>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="from">依赖注入源类型</param>
        /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public object ResolveRequired(string? key, Type from, IParameter[]? parameters = null)
        {
            object? value = manager.Resolve(key, from, parameters);
            ThrowIfNull(value, $"构建实例失败：Resolve返回null。key:{key ?? STR_Null};from:{from.FullName}");
            return value!;
        }

        /// <summary>
        /// 构建泛型实例
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
        /// <returns>构建成功的实例；否则返回null</returns>
        public T? Resolve<T>(string? key = null, IParameter[]? parameters = null)
            => (T?)manager.Resolve(key, typeof(T), parameters);
        /// <summary>
        /// 构建有效实例，返回null报错
        /// </summary>
        /// <typeparam name="T">依赖注入源类型</typeparam>
        /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
        /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
        /// <returns>构建成功的实例；否则抛出异常</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public T ResolveRequired<T>(string? key = null, IParameter[]? parameters = null)
        {
            object value = manager.ResolveRequired(key, typeof(T), parameters);
            return (T)value;
        }
        #endregion
    }
}
