using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Dependency.DataModels;

/// <summary>
/// 依赖注入参数对象
/// </summary>
/// <typeparam name="T"></typeparam>
public class Parameter<T> : IParameter<T>
{
    #region 属性变量
    /// <summary>
    /// 参数名
    /// </summary>
    public readonly string? Name;
    /// <summary>
    /// 参数值
    /// </summary>
    public readonly T Value;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// <para>name使用null</para>
    /// </summary>
    /// <param name="value">参数值</param>
    public Parameter(T value) : this(name: null, value) { }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="name">参数名</param>
    /// <param name="value">参数值</param>
    public Parameter(string? name, T value)
    {
        Name = name;
        Value = value;
    }
    #endregion

    #region IParameter
    /// <summary>
    /// 参数名称
    /// </summary>
    string? IParameter.Name => Name;
    /// <summary>
    /// 取参数值
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    T? IParameter<T>.GetParameter(in IDIManager manager) => Value;
    #endregion
}
