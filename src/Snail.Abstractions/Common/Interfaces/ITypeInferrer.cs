namespace Snail.Abstractions.Common.Interfaces;
/// <summary>
/// 接口约束：类型推断器
/// <para>1、根据数据特征（如key、value）推测属于哪个类型</para>
/// <para>2、如反序列化数据到类型为abstract/interface的属性字段时，需要得到实际数据类型才能反序列化成功</para>
/// </summary>
public interface ITypeInferrer
{
    /// <summary>
    /// 支持哪些类型推断其实现类
    /// <para>1、如Ilist属性，反序列化时，需要推断实际使用List还是其他实现类承载数据</para>
    /// </summary>
    Type[] SupportTypes { get; }

    /// <summary>
    /// 推断类型
    /// <para>1、推断<paramref name="type"/>对应的接口、抽象类在反序列化时，映射为具体的实现类</para>
    /// </summary>
    /// <param name="type">要推断具体实现类的类型，如<see cref="IList{T}"/>具体应该由哪个派生类来实现</param>
    /// <param name="keyFunc">判断是否包含传入的指定key；包含则判定为container，否则为filter</param>
    /// <param name="valueFunc">特定情况下，需要取指定Key的数据，做处理；内部会强制调用ToString转成字符串</param>
    /// <returns>推断失败则返回null</returns>
    Type? InferType(Type type, Func<string, bool> keyFunc, Func<string, object?> valueFunc);
}
