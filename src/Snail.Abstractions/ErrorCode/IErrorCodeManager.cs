using Snail.Abstractions.ErrorCode.Interfaces;

namespace Snail.Abstractions.ErrorCode
{
    /// <summary>
    /// 接口约束：错误编码管理器
    /// </summary>
    public interface IErrorCodeManager
    {
        /// <summary>
        /// 注册错误编码信息；确保<see cref="IErrorCode.Code"/>唯一，重复注册以第一个为准
        /// </summary>
        /// <param name="culture">语言环境；传null则走默认zh-CN</param>
        /// <param name="errors">错误编码集合</param>
        /// <returns>管理器自身，方便链式调用</returns>
        IErrorCodeManager Register(string? culture, params IList<IErrorCode> errors);

        /// <summary>
        /// 根据错误编码信息，获取具体的错误信息对象 <br />
        ///     1、若自身<paramref name="culture"/>找不到，则尝试从zh-CN查找
        /// </summary>
        /// <param name="culture">语言环境；传null则走默认zh-CN</param>
        /// <param name="code">错误编码</param>
        /// <returns>编码信息</returns>
        IErrorCode? Get(string? culture, string code);
    }
}
