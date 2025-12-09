namespace Snail.Abstractions.Dependency.Interfaces;

/// <summary>
/// 接口约束：要注入的组件相关实现信息
/// </summary>
public interface IComponent
{
    /// <summary>
    /// 依赖注入Key值，用于DI动态构建实例 <br />
    ///     1、用于区分同一个源（From）多个实现（to）的情况 <br />
    /// </summary>
    /// <remarks>虽然和<see cref="IInject"/>中Key值意义一样，也不继承，避免接口串了</remarks>
    string? Key { get; }

    /// <summary>
    /// 源类型：当前组件实现哪个类型 <br />
    ///     1、为null时取自身作为From；不分析接口、基类等，如实现了IDisposable等系统接口，分析了占地方 <br />
    /// </summary>
    Type? From { get; }

    /// <summary>
    /// 组件生命周期，默认【瞬时】
    /// </summary>
    LifetimeType Lifetime { get; }
}
