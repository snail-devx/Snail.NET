using Snail.Abstractions.ErrorCode.Exceptions;
using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.Abstractions.ErrorCode.Extensions;

/// <summary>
/// <see cref="IErrorCodeManager"/>扩展方法
/// </summary>
public static class ErrorCodeExtensions
{
    #region 公共方法
    /// <summary>
    /// 注册错误编码信息；若code重合则覆盖旧的
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="errors">错误编码集合</param>
    /// <returns>管理器自身，方便链式调用</returns>
    public static IErrorCodeManager Register(this IErrorCodeManager manager, IList<IErrorCode> errors)
        => manager.Register(culture: null, errors);

    /// <summary>
    /// 根据错误编码信息，获取具体的错误信息对象
    /// <para>1、从zh-CN查找 </para>
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="code">错误编码</param>
    /// <returns>编码信息</returns>
    public static IErrorCode? Get(this IErrorCodeManager manager, string code)
        => manager.Get(culture: null, code);
    /// <summary>
    /// 根据错误编码信息，获取具体的错误信息对象
    /// <para>1、从zh-CN查找 </para>
    /// <para>2、找不到报错 </para>
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="code">错误编码</param>
    /// <returns>编码信息</returns>
    /// <returns></returns>
    public static IErrorCode GetRequired(this IErrorCodeManager manager, string code)
        => GetRequired(manager, culture: null, code);
    /// <summary>
    /// 根据错误编码信息，获取具体的错误信息对象
    /// <para>1、若自身<paramref name="culture"/>找不到，则尝试从zh-CN查找 </para>
    /// <para>2、找不到报错 </para>
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="culture">语言环境；传null则走默认zh-CN</param>
    /// <param name="code">错误编码</param>
    /// <returns>编码信息</returns>
    /// <returns></returns>
    public static IErrorCode GetRequired(this IErrorCodeManager manager, string? culture, string code)
    {
        IErrorCode? error = manager.Get(culture, code);
        if (error == null)
        {
            string msg = $"错误编码未注册；请排查。ErrorCode:{code}";
            throw new ApplicationException(msg);
        }
        return error;
    }
    /// <summary>
    /// 获取错误编码对应的异常对象
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="culture">语言环境；传null则走默认zh-CN</param>
    /// <param name="code">错误编码</param>
    /// <returns></returns>
    public static ErrorCodeException GetException(this IErrorCodeManager manager, string? culture, string code)
    {
        IErrorCode error = GetRequired(manager, culture, code);
        return new ErrorCodeException(error);
    }
    #endregion
}
