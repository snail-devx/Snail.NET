using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Logging.DataModels;

namespace Snail.Logging.DataModels;

/// <summary>
/// 性能日志描述器
/// </summary>
public class PerformanceLogDescriptor : LogDescriptor, IPerformance
{
    #region IPerformanceDescriptor
    /// <summary>
    /// 请求耗时毫秒
    /// </summary>
    public long? Performance { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="forceLog">此日志是否为强制日志；强制日志，不受系统配置日志层级控制，始终记录</param>
    public PerformanceLogDescriptor(bool forceLog = false) : base(forceLog) { }
    #endregion
}
