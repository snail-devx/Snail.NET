using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.Attributes
{
    /// <summary>
    /// 特性标签：组件实现相关信息 <br />
    /// 1、用于程序启动时做自动扫描依赖注入，减少配置文件使用 <br />
    /// 2、支持Key值模式和工作空间模式
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ComponentAttribute : Attribute, IComponent
    {
        #region IComponent
        /// <summary>
        /// 依赖注入Key值，用于DI动态构建实例 <br />
        /// 1、用于区分同一个源（From）多个实现（to）的情况 <br />
        /// 2、默认值为null
        /// </summary>
        public string? Key { init; get; }

        /// <summary>
        /// 源类型：当前组件实现哪个类型 <br />
        ///     1、为null时取自身作为From；不分析接口、基类等，如实现了IDisposable等系统接口，分析了占地方 <br />
        /// </summary>
        public Type? From { init; get; }

        /// <summary>
        /// 组件生命周期，默认【单例】
        /// </summary>
        public LifetimeType Lifetime { init; get; } = LifetimeType.Singleton;
        #endregion
    }

    /// <summary>
    /// 特性标签：组件实现相关信息 <br />
    /// 1、用于程序启动时做自动扫描依赖注入，减少配置文件使用 <br />
    /// 2、支持Key值模式和工作空间模式
    /// </summary>
    /// <typeparam name="FromType">依赖注入源类型</typeparam>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ComponentAttribute<FromType> : Attribute, IComponent
    {
        #region IComponent
        /// <summary>
        /// 依赖注入Key值，用于DI动态构建实例 <br />
        /// 1、用于区分同一个源（From）多个实现（to）的情况 <br />
        /// 2、默认值为null
        /// </summary>
        public string? Key { init; get; }

        /// <summary>
        /// 源类型：当前组件实现哪个类型 <br />
        /// 1、为null时自动将自己作为From <br />
        /// </summary>
        Type IComponent.From => typeof(FromType);

        /// <summary>
        /// 组件生命周期，默认【单例】
        /// </summary>
        public LifetimeType Lifetime { init; get; } = LifetimeType.Singleton;
        #endregion
    }
}
