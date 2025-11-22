using Snail.Abstractions.ErrorCode.DataModels;
using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.WebApp.DataModels;

/// <summary>
/// 错误异常详细信息描述器
/// <para>代码具体异常信息</para>
/// </summary>
public class ErrorCodeDetailDescriptor : ErrorCodeDescriptor
{
    /// <summary>
    /// 异常类型：异常名称
    /// </summary>
    public required string Type { init; get; }
    /// <summary>
    /// 发生错误时的详细信息：<see cref="Exception.Message"/>
    ///     仅限内部使用，不开放给外部
    ///     将具体的错误信息记录出来返回，方便基于此做错误排查
    /// </summary>
    public required string Detail { init; get; }

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="error"></param>
    public ErrorCodeDetailDescriptor(IErrorCode error) : base(error.Code, error.Message)
    {
    }
}