using Snail.Abstractions.Common.Interfaces;

namespace Snail.Abstractions.Common.DataModels;
/// <summary>
/// 数据包对象
/// </summary>
/// <typeparam name="T"></typeparam>
public class DataBag<T> : IDataBag<T>
{
    #region 属性变量
    /// <summary>
    /// 数据对象
    /// </summary>
    private T? _data;
    #endregion

    #region IDataBag<T>
    /// <summary>
    /// 获取数据
    /// </summary>
    /// <returns></returns>
    T? IDataBag<T>.GetData() => _data;
    /// <summary>
    /// 设置数据
    /// </summary>
    /// <param name="data"></param>
    void IDataBag<T>.SetData(T? data) => _data = data;
    #endregion
}
