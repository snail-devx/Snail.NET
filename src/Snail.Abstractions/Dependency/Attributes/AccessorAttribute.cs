using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.Attributes;

/// <summary>
/// 特性标签：访问器依赖注入
/// <para>1、用于依赖注入自动构建<typeparamref name="IAccessor"/>实例 </para>
/// <para>2、配置<typeparamref name="IProvider"/>参数注入Key，动态构建实例作为<typeparamref name="IAccessor"/>构造方法参数值传入  </para>
/// <para>3、暂时不集成服务器地址配置选项参数注入 </para>
/// </summary>
/// <typeparam name="IAccessor">访问器，如日志记录器，负责对外提供服务，内部调用日志提供程序完成日志记录</typeparam>
/// <typeparam name="IProvider">访问器提供程序，如日志提供程序负责具体日志记录</typeparam>
/// <remarks>无实际意义<br />
///     1、可通过<see cref="InjectAttribute"/>和<see cref="ParameterAttribute{Provider}"/>组装完成相同功能<br />
///     2、方便Loging、Http、Message等这类访问器+提供程序架构模式，做依赖注入，在这里抽取的简化版注入标签<br />
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class AccessorAttribute<IAccessor, IProvider> : Attribute, IInject, IParameter<IProvider>
{
    #region 属性变量
    /// <summary>
    /// <typeparamref name="IAccessor"/>访问器依赖注入Key值
    /// </summary>
    public string? Key { init; get; }

    /// <summary>
    /// <typeparamref name="IProvider"/>提供程序依赖注入key值
    /// </summary>
    public string? ProviderKey { init; get; }
    #endregion

    #region IInject
    /// <summary>
    /// 依赖注入Key值，用于DI动态构建实例
    /// <para>1、用于区分同一个源（From）多个实现（to）的情况 </para>
    /// <para>2、默认值为null </para>
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    string? IInject.GetKey(IDIManager manager) => Key;
    #endregion

    #region IParameter
    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    IProvider? IParameter<IProvider>.GetParameter(in IDIManager manager)
        => manager.Resolve<IProvider>(key: ProviderKey);
    #endregion
}
