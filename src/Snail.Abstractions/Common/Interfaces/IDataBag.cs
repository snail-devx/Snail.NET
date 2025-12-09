namespace Snail.Abstractions.Common.Interfaces;

/// <summary>
/// 数据包：对指定类型数据的包裹
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataBag<T>
{
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <returns></returns>
    T? GetData();
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="data"></param>
    void SetData(T? data);
}
