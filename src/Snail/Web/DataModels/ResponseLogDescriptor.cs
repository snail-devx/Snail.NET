using Snail.Abstractions.Logging.DataModels;

namespace Snail.Web.DataModels;

/// <summary>
/// Http请求响应日志
/// </summary>
public sealed class ResponseLogDescriptor : LogDescriptor, IPerformance
{
    #region 属性变量
    /// <summary>
    /// 响应Headers
    /// </summary>
    public Dictionary<string, string?>? Headers { init; get; }
    /// <summary>
    /// 请求耗时毫秒
    /// </summary>
    public long? Performance { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public ResponseLogDescriptor() : base()
    { }
    #endregion
}
