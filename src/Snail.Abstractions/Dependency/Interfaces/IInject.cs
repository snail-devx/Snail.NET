namespace Snail.Abstractions.Dependency.Interfaces;

/// <summary>
/// 接口约束：经过DI构建注入组件信息
/// </summary>
public interface IInject
{
    /// <summary>
    /// 依赖注入Key值，用于DI动态构建实例
    /// <para>1、用于区分同一个源（From）多个实现（to）的情况 </para>
    /// <para>2、默认值为null </para>
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    string? GetKey(IDIManager manager);
}
