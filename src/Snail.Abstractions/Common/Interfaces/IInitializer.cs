namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 接口约束：初始化器
/// <para>1、作为基础接口存在，满足Message、Http等组件初始化配置需求</para>
/// <para>2、如Message、Http等在管理器实例构建时，执行初始化器，固化中间件逻辑等</para>
/// </summary>
public interface IInitializer<T>
{
    /// <summary>
    /// 初始化对象；进行对象的预配置操作
    /// </summary>
    /// <param name="obj"></param>
    void Initialize(T obj);
}