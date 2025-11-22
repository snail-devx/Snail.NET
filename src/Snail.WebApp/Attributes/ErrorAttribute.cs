using Snail.Abstractions.ErrorCode.DataModels;
using Snail.Abstractions.ErrorCode.Interfaces;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Attributes;

/// <summary>
/// 特性标签：API发生错误时的处理
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ErrorAttribute : Attribute, IActionAttribute
{
    /// <summary>
    /// 是否禁用错误处理
    /// <para>1、为false时，会捕捉错误信息，封装<seealso cref="IErrorCode"/>对象返回，用以实现错误标准化</para>
    /// <para>2、为true时，调用方接收到的500错误</para>
    /// </summary>
    public bool Disabled { init; get; }

    /// <summary>
    /// 错误编码
    /// <para>1、<see cref="Disabled"/>为false时生效</para>
    /// <para>2、拦截到错误时，基于此编码构建<see cref="ErrorCodeDescriptor"/>对象</para>
    /// <para>3、为null则使用-1 未知错误；传了编码，必须确保存在，否则仍然会报错</para>
    /// </summary>
    public string? ErrorCode { init; get; }
}
