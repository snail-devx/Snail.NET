using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Enumerations;
using Snail.Utilities.Common.Utils;

namespace Snail.Abstractions.Logging.Extensions;

/// <summary>
/// <see cref="ILogger"/>扩展方法
/// </summary>
public static class LoggerExtensions
{
    #region 属性变量
    /// <summary>
    /// 自身类型
    /// </summary>
    private static readonly Type _type = typeof(LoggerExtensions);
    #endregion

    #region 公共方法
    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="level">日志级别</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Log(this ILogger logger, LogLevel level, string title, string? content = null, Exception? ex = null)
    {
        //  如果日志级别无效，将不记录日志；但返回true
        if (logger.IsEnable(level) == false)
        {
            return true;
        }
        //  构建日志描述器：取堆栈信息，分析非LogManager的调用入口
        MethodBase? entryMethod = DiagnosticsHelper.GetEntryMethod(_type);
        LogDescriptor descriptor = new LogDescriptor()
        {
            Level = level,
            Title = title,
            Content = content,

            AssemblyName = entryMethod!.DeclaringType!.Assembly.FullName,
            ClassName = entryMethod.DeclaringType.FullName,
            MethodName = entryMethod.Name,

            Exception = ex
        };

        return logger.Log(descriptor);
    }
    /// <summary>
    /// 记录【Trace】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Trace(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.Trace, title, content, ex);
    /// <summary>
    /// 记录【Debug】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Debug(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.Debug, title, content, ex);
    /// <summary>
    /// 记录【Info】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Info(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.Info, title, content, ex);
    /// <summary>
    /// 记录【Warn】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Warn(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.Warn, title, content, ex);
    /// <summary>
    /// 记录【Error】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool Error(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.Error, title, content, ex);
    /// <summary>
    /// 记录【System】级别日志
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="title">日志标题</param>
    /// <param name="content">日志数据</param>
    /// <param name="ex">异常对象；未发生异常，传null即可</param>
    /// <returns>记录成功返回true；否则false</returns>
    public static bool System(this ILogger logger, string title, string? content = null, Exception? ex = null)
        => Log(logger, LogLevel.System, title, content, ex);
    #endregion
}
