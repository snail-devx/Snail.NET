namespace Snail.Dependency.Interfaces;

/// <summary>
/// 接口：依赖注入类型存储器<br />
///     1、存储实现类实例信息，和依赖注入生命周期绑定 <br />
/// </summary>
internal interface ITypeStorager
{
    /// <summary>
    /// 基于当前存储器，构建新的存储器实例<br />
    ///     1、在<see cref="IDIManager.New"/>执行时，会依赖注入的实例存储器做继承处理，从而维护完整的生命周期
    /// </summary>
    /// <returns>新的存储器实例，若无需继承，则返回null即可</returns>
    ITypeStorager? New();

    /// <summary>
    /// 获取依赖实例对象
    /// </summary>
    /// <returns>若返回null，则DI需要全新构建</returns>
    object? GetInstace();

    /// <summary>
    /// 保存实例对象
    /// </summary>
    /// <param name="instance">DI构建的实例对象</param>
    void SaveInstace(in object? instance);

    /// <summary>
    /// 尝试实例销毁存储器
    /// </summary>
    void TryDestroy();
}
