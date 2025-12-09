using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.Abstractions.ErrorCode.Exceptions;

/// <summary>
/// 错误编码异常
/// </summary>
public sealed class ErrorCodeException : Exception
{
    #region 属性变量
    /// <summary>
    /// 错误编码对象
    /// </summary>
    public readonly IErrorCode ErrorCode;

    /// <summary>
    /// 异常消息
    /// </summary>
    public override string Message => $"{ErrorCode.Code};{ErrorCode.Message}";
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="error"></param>
    public ErrorCodeException(IErrorCode error) : base(null)
    {
        ErrorCode = ThrowIfNull(error);
    }
    #endregion
}
