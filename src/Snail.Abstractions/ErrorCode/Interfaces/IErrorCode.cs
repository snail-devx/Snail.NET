namespace Snail.Abstractions.ErrorCode.Interfaces;

/// <summary>
/// 接口约束：错误编码
/// </summary>
public interface IErrorCode
{
    /// <summary>
    /// 错误编码
    /// </summary>
    string Code { get; }
    /// <summary>
    /// 具体错误消息
    /// </summary>
    string Message { get; }
}
