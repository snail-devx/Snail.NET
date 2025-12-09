using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.Abstractions.ErrorCode.DataModels;

/// <summary>
/// 错误编码描述器
/// </summary>
public class ErrorCodeDescriptor : IErrorCode
{
    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="code">错误编码</param>
    /// <param name="message">具体错误消息</param>
    public ErrorCodeDescriptor(string code, string message)
    {
        Code = ThrowIfNullOrEmpty(code);
        Message = ThrowIfNullOrEmpty(message);
    }
    #endregion

    #region IErrorCode
    /// <summary>
    /// 错误编码
    /// </summary>
    public string Code { set; get; }
    /// <summary>
    /// 具体错误消息
    /// </summary>
    public string Message { set; get; }
    #endregion
}
