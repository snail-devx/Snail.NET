namespace Snail.Abstractions.Dependency.Enumerations
{
    /// <summary>
    /// 生命周期类型
    /// </summary>
    public enum LifetimeType
    {
        /// <summary>
        /// 单例模式：全局只有一个对象
        /// </summary>
        Singleton = 0,

        /// <summary>
        /// 容器单例模式：当前Manager下只有一个对象
        /// </summary>
        Scope = 10,

        /// <summary>
        /// 瞬时：每次全新构建
        /// </summary>
        Transient = 20,
    }
}
