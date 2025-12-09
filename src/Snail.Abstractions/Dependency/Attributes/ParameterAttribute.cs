using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.Attributes;

/// <summary>
/// 特性标签：【依赖注入】构建实例时的构造方法注入参数值 <br />
///     1、基于DI构建<typeparamref name="T"/>类型参数值 <br />
///     2、仅在【属性、字段、方法参数】的标签上生效
/// </summary>
/// <typeparam name="T">源类型</typeparam>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
public class ParameterAttribute<T> : Attribute, IParameter
{
    #region 属性变量
    /// <summary>
    /// <typeparamref name="T"/>的依赖注入Key值，用于DI动态构建实例
    /// </summary>
    public string? Key { init; get; }
    #endregion

    #region IParameter
    /// <summary>
    /// 参数类型：和<see cref="Name"/>配合时用，选举要传递信息的目标参数 <br />
    /// </summary>
    Type IParameter.Type => typeof(T);

    /// <summary>
    /// 参数名称：和<see cref="IParameter.Type"/>配合使用，选举要传递信息的目标参数 <br />
    /// 1、Name为空时，则选举第一个类型为Type的参数
    /// 2、Name非空时，则选举类型为Type、且参数名为Name的参数
    /// </summary>
    public string? Name { init; get; }

    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <returns></returns>
    public object? GetParameter(in IDIManager manager) => manager.Resolve(key: Key, typeof(T));
    #endregion
}
