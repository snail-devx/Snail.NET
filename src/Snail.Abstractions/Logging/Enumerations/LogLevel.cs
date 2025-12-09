namespace Snail.Abstractions.Logging.Enumerations;

/// <summary>
/// 日志等级枚举
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// 跟踪日志；如性能跟踪；调用方法前后性能追踪等
    /// </summary>
    Trace = 0,
    /// <summary>
    /// 调试
    /// </summary>
    Debug = 10,

    /// <summary>
    /// 信息日志。程序正常运行时使用；正常的流水信息记录
    /// </summary>
    Info = 20,

    /// <summary>
    /// 警告日志。程序未按预期运行时使用，但并不是错误，如:用户登录密码错误
    /// </summary>
    Warn = 30,

    /// <summary>
    /// 错误日志；程序出错误时使用，如:IO操作失败
    /// </summary>
    Error = 40,

    /// <summary>
    /// 系统级别日志；程序验证问题，如磁盘已满；删除业务数据时的日志留存等
    /// </summary>
    System = 50,
}
