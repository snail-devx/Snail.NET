namespace Snail.Abstractions.Dependency.Interfaces;

/// <summary>
/// 接口约束：依赖注入的参数，在DI构建实例时，传递给实现类型的【构造方法】的参数
/// <para>1、仅在【属性、字段、方法参数】的标签上生效，将标记的参数传递到对应实现类的构造方法中 </para>
/// <para>2、自定义实现<see cref="GetParameter(in IDIManager)"/>方法；可基于<see cref="IDIManager.Resolve(string?, Type)"/>动态构建值，也可固定值等等 </para>
/// </summary>
public interface IParameter
{
    /// <summary>
    /// 参数类型：和<see cref="Name"/>配合时用，选举要传递信息的目标参数
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// 参数名称：和<see cref="Type"/>配合使用，选举要传递信息的目标参数
    /// <para>1、Name为空时，则选举第一个类型为Type的参数 </para>
    /// <para>2、Name非空时，则选举类型为Type、且参数名为Name的参数 </para>
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    object? GetParameter(in IDIManager manager);
}

/// <summary>
/// 接口约束：依赖注入的参数，在DI构建实例时，传递给实现类型的【构造方法】的参数
/// <para>1、仅在【属性、字段、方法参数】的标签上生效，将标记的参数传递到对应实现类的构造方法中 </para>
/// <para>2、自定义实现<see cref="GetParameter(in IDIManager)"/>方法；可基于<see cref="IDIManager.Resolve(string?, Type)"/>动态构建值，也可固定值等等 </para>
/// </summary>
/// <typeparam name="T">参数类型</typeparam>
public interface IParameter<T> : IParameter
{
    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    new T? GetParameter(in IDIManager manager);

    #region IParameter 做一下默认实现
    /// <summary>
    /// 参数类型：和<see cref="IParameter.Name"/>配合时用，选举要传递信息的目标参数
    /// </summary>
    Type IParameter.Type => typeof(T);
    /// <summary>
    /// 参数名称：和<see cref="IParameter.Type"/>配合使用，选举要传递信息的目标参数
    /// <para>1、Name为空时，则选举第一个类型为Type的参数 </para>
    /// <para>2、Name非空时，则选举类型为Type、且参数名为Name的参数 </para>
    /// </summary>
    string? IParameter.Name => null;

    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    object? IParameter.GetParameter(in IDIManager manager)
        => GetParameter(manager);
    #endregion
}
