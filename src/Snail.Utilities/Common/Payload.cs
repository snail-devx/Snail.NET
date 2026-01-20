using Snail.Utilities.Common.Interfaces;

namespace Snail.Utilities.Common;
/// <summary>
/// 有效载荷数据
/// </summary>
/// <typeparam name="T">负载的数据类型</typeparam>
public class Payload<T> : IPayload<T>
{
    #region 属性变量
    /// <summary>
    /// 有效载荷数据
    /// </summary>
    private T? _payload;
    #endregion

    #region IPayload<T>
    /// <summary>
    /// 有效载荷数据
    /// </summary>
    T? IPayload<T>.Payload { get => _payload; set => _payload = value; }
    #endregion
}