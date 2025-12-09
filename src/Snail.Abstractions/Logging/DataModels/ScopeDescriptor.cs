namespace Snail.Abstractions.Logging.DataModels;

/// <summary>
/// 日志作用域描述器
/// </summary>
public class ScopeDescriptor
{
    /// <summary>
    /// 上下文操作Id
    /// </summary>
    public required string ContextId { init; get; }
    /// <summary>
    /// 日志父级Id，在<see cref="ILogger.Scope(string, string?)"/>后生成
    /// </summary>
    public string? ParentId { init; get; }
}
