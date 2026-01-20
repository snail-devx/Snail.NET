using Snail.Abstractions.Logging.DataModels;
using Snail.Utilities.Common.Interfaces;

namespace Snail.WebApp.DataModels;

/// <summary>
/// API动作开始日志描述器
/// </summary>
public sealed class ActionExecutingLogDescriptor : LogDescriptor
{
    /// <summary>
    /// 请求方法
    /// </summary>
    public string? HttpMethod { get; init; }

    /// <summary>
    /// 请求Headers
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// 请求URL地址
    /// </summary>
    public string? RequestURL { get; init; }
}
/// <summary>
/// API动作完成日志描述器
/// </summary>
public class ActionExecutedLogDescriptor : LogDescriptor, IPerformance
{
    /// <summary>
    /// 请求耗时毫秒
    /// </summary>
    public long? Performance { set; get; }
}
