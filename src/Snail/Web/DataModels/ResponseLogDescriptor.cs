using Snail.Logging.DataModels;

namespace Snail.Web.DataModels;

/// <summary>
/// Http请求响应日志
/// </summary>
public sealed class ResponseLogDescriptor : PerformanceLogDescriptor
{
    #region 属性变量
    /// <summary>
    /// 响应Headers
    /// </summary>
    public Dictionary<string, string?>? Headers { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public ResponseLogDescriptor() : base()
    { }
    #endregion
}
