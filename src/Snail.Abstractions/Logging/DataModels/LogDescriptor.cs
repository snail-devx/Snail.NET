using Snail.Abstractions.Logging.Enumerations;

namespace Snail.Abstractions.Logging.DataModels;

/// <summary>
/// 日志信息描述器
/// </summary>
public class LogDescriptor
{
    #region 属性变量
    /// <summary>
    /// 是否为强制日志
    ///     此日志是否为强制日志；强制日志，不受系统配置日志层级控制，始终记录
    ///     此属性非常规使用属性，加下划线区分一下
    /// </summary>
    internal bool IsForce { private init; get; }

    /// <summary>
    /// 日志等级
    /// </summary>
    public required LogLevel Level { init; get; }
    /// <summary>
    /// 日志标题
    /// </summary>
    public required string Title { init; get; }
    /// <summary>
    /// 日志标签：用于进行细致化区分使用，如HTTP日志，Api日志，MQ日志、、、
    /// </summary>
    public string? LogTag { init; get; }
    /// <summary>
    /// 具体的日志内容信息
    /// </summary>
    public string? Content { init; get; }

    /// <summary>
    /// 程序集名称：哪个程序集下发起的日志记录
    /// </summary>
    public string? AssemblyName { init; get; }
    /// <summary>
    /// 类名称：哪个类下发起的日志记录
    /// </summary>
    public string? ClassName { init; get; }

    /// <summary>
    /// 方法名：哪个方法下发起的日志记录
    /// </summary>
    public string? MethodName { init; get; }

    /// <summary>
    /// 异常信息：未发生异常，则为null
    /// </summary>
    public Exception? Exception { init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 默认无参构造方法
    /// </summary>
    public LogDescriptor() { }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="forceLog">此日志是否为强制日志；强制日志，不受系统配置日志层级控制，始终记录</param>
    public LogDescriptor(bool forceLog) => IsForce = forceLog;
    #endregion
}
