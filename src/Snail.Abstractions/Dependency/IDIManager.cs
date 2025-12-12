using Snail.Abstractions.Dependency.DataModels;
using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency;

/// <summary>
/// 接口：依赖注入管理器；约束依赖注入管理器的基础行为
/// </summary>
public interface IDIManager : IDisposable
{
    /// <summary>
    /// 管理器销毁事件
    /// </summary>
    event Action OnDestroy;

    /// <summary>
    /// 基于当前管理器构建新管理实例
    /// <para>1、继承当前管理器中已注册的依赖注入信息 </para>
    /// <para>2、相当于继承当前管理器，创建一个全新的子管理器实例 </para>
    /// </summary>
    /// <returns></returns>
    IDIManager New();

    /// <summary>
    /// 判断指定类型是否注册了
    /// </summary>
    /// <param name="key">依赖注入Key值</param>
    /// <param name="from">依赖注入源类型</param>
    /// <returns>已注册返回true；否则返回false</returns>
    bool IsRegistered(string? key, Type from);
    /// <summary>
    /// 注册依赖注入信息
    /// </summary>
    /// <param name="descriptors"></param>
    /// <returns></returns>
    IDIManager Register(params IList<DIDescriptor> descriptors);
    /// <summary>
    /// 尝试注册依赖注入信息；已存在则不注册了
    /// </summary>
    /// <param name="descriptor">依赖注入信息，分析<see cref="DIDescriptor.Key"/>和<see cref="DIDescriptor.From"/>判断是否已经注册过了</param>
    /// <returns>是否注册成功</returns>
    bool TryRegister(DIDescriptor descriptor);
    /// <summary>
    /// 反注册符合条件的依赖注入信息
    /// </summary>
    /// <param name="key">依赖注入Key值</param>
    /// <param name="from">依赖注入源类型</param>
    /// <returns>返回自身，方便链式调用</returns>
    IDIManager Unregister(string? key, Type from);

    /// <summary>
    /// 依赖注入构建实例
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <returns>构建完成的实例对象</returns>
    object? Resolve(string? key, Type from);
    /// <summary>
    /// 依赖注入构建实例
    /// </summary>
    /// <param name="key">依赖注入Key值，用于DI动态构建实例</param>
    /// <param name="from">源类型：基于此查找注册信息</param>
    /// <param name="parameters">实现类型的构造方法执行时注入的参数信息</param>
    /// <returns>构建完成的实例对象</returns>
    /// <remarks>此方法内部使用，暂不对外</remarks>
    internal object? Resolve(string? key, Type from, IParameter[] parameters);
}
