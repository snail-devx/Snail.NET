namespace Snail.Abstractions.Web.Attributes;

/// <summary>
/// 特性标签；HTTP请求日志特性标签<br />
///     1、调用方需要特定控制日志记录数据时，在Func、Action、Method上加上此标签<br />
///     2、使用此标签前，需要先启用日志中间件<br />
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
public sealed class HttpLogAttribute : Attribute
{
    /// <summary>
    /// 是否记录发送数据
    /// </summary>
    public bool Send { init; get; } = true;
    /// <summary>
    /// 是否记录请求结果数据
    /// </summary>
    public bool Response { init; get; } = true;

    ///// <summary>
    ///// 是否记录请求错误数据
    ///// 先不放开，始终记录
    ///// </summary>
    //public bool Error { init; get; } = true;

    /// <summary>
    /// 是否记录性能日志：请求耗时
    /// </summary>
    public bool Performance { init; get; } = true;
}
