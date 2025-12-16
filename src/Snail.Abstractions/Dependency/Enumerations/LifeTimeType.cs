namespace Snail.Abstractions.Dependency.Enumerations;

/// <summary>
/// 生命周期类型
/// </summary>
public enum LifetimeType
{
    /// <summary>
    /// 单例模式
    /// <para>1、全局只有一个对象</para>
    /// <para>2、如<see cref="IApplication"/>实例下全局只有一个对象；不同<see cref="IApplication"/>实例下隔离</para>
    /// </summary>
    Singleton = 0,

    /// <summary>
    /// 容器单例模式
    /// <para>1、每个<see cref="IDIManager"/>下只有一个对象</para>
    /// <para>2、如ASP.NET Core的每个HTTP请求就会构建一个全新的<see cref="IDIManager"/></para>
    /// </summary>
    Scope = 10,

    /// <summary>
    /// 瞬时模式
    /// <para>1、每次全新构建</para>
    /// </summary>
    Transient = 20,
}
