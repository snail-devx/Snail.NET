namespace Snail.Utilities.Common.Interfaces;
/// <summary>
/// 接口约束：有效载荷数据
/// <para>1、约束核心载荷数据，实现类可再扩展非核心数据</para>
/// </summary>
/// <typeparam name="T">负载的数据类型</typeparam>

public interface IPayload<T>
{
    /// <summary>
    /// 有效载荷数据
    /// </summary>
    T? Payload { get; set; }
}