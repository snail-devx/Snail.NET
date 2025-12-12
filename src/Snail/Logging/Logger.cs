using Snail.Abstractions.Identity;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Enumerations;
using Snail.Abstractions.Logging.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using Snail.Logging.DataModels;

namespace Snail.Logging;

/// <summary>
/// 日志记录器
/// </summary>
[Component<ILogger>(Lifetime = LifetimeType.Transient)]
public sealed class Logger : ILogger
{
    #region 属性变量
    /// <summary>
    /// 默认的日志等级
    /// </summary>
    private readonly LogLevel _level;
    /// <summary>
    /// 日志服务器配置选项
    /// </summary>
    private readonly IServerOptions? _server;
    /// <summary>
    /// 日志提供程序
    /// </summary>
    private readonly ILogProvider _provider;
    /// <summary>
    /// 主键Id生成器
    /// </summary>
    private readonly IIdGenerator _idGenerator;

    /// <summary>
    /// 日志记录器作用域
    /// </summary>
    private readonly ScopeDescriptor? _scope;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="server">日志服务器配置选项；为null提供程序自身做默认值处理，或者报错 <br />
    ///     1、记录器为网络日志时，日志要记录到哪个服务器下，如哪个数据库服务器 <br />
    ///     2、记录器为本地日志时，采用哪个工作组下的配置，如log4net配置；此时仅<see cref="IServerOptions.Workspace"/>生效 <br />
    /// </param>
    /// <param name="provider">日志记录提供程序，为null则表示系统系统默认；除非想做定制，否则忽略即可</param>
    [Inject]
    public Logger(IApplication app, IServerOptions? server = null, ILogProvider? provider = null)
    {
        ThrowIfNull(app);
        //  通过配置分析日志级别：生产环境无配置，默认Info；开发环境无配置，默认Trace
        try
        {
            string? logLevel = Default(app.GetEnv("LogLevel"), null);
            _level = logLevel == null
                ? app.IsProduction ? LogLevel.Info : LogLevel.Trace
                : logLevel.AsEnum<LogLevel>();
        }
        catch
        {
            _level = LogLevel.Info;
        }

        _server = server;
        _provider = provider ?? app.ResolveRequired<ILogProvider>();
        _idGenerator = app.ResolveRequired<IIdGenerator>();
    }
    /// <summary>
    /// 基于父级logger创建子级作用域logger
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="scope"></param>
    private Logger(Logger parent, ScopeDescriptor scope)
    {
        _level = parent._level;
        _server = parent._server;
        _provider = parent._provider;
        _idGenerator = parent._idGenerator;
        _scope = scope;
    }
    #endregion

    #region ILogger
    /// <summary>
    /// 日志级别是否可用；不可用将不记录此级别日志
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="forceLog">是否是【强制】日志</param>
    /// <returns>可用返回true；否则返回false</returns>
    public bool IsEnable(LogLevel level, bool forceLog = false)
    {
        /*  强制日志，不管级别是啥都记录；非强制日志：1、日志等级匹配，2、未全局禁用日志 */
        return forceLog == true
            ? true
            : (RunContext.Current.DisableLog == false && level >= _level);
    }

    /// <summary>
    /// 记录日志
    /// </summary>
    /// <param name="descriptor">要记录的日志信息</param>
    /// <returns>记录成功；返回true</returns>
    bool ILogger.Log(LogDescriptor descriptor)
    {
        //  是否能够记录日志，级别是否构，不够则忽略掉
        if (IsEnable(descriptor.Level, descriptor.IsForce) == true)
        {
            ScopeDescriptor scope = BuildLogScope(this);
            return _provider.Log(descriptor, scope, _server);
        }
        return true;
    }

    /// <summary>
    /// 创建新作用域日志记录器 <br />
    ///     1、基于<paramref name="title"/>创建一条唯一主键Id标记日志，后续日志将归属于此Id组，和其他日志区分开<br />
    ///     2、一般在进行多线程操作时，子线程之间日志做归组使用，方便查看日志层级<br />
    /// </summary>
    /// <param name="title">日志标题；将作为后续日志组的组名</param>
    /// <param name="content">日志内容；日志组日志的内容信息</param>
    /// <returns>新的日志管理，此管理器下的日志合并到同一组中</returns>
    ILogger ILogger.Scope(string title, string? content)
    {
        //  生成【强制】日志，记录日志主键Id值（Id用默认生成？）
        string logId = _idGenerator.NewId("Logging");
        ScopeDescriptor scope = BuildLogScope(this);
        _provider.Log(new IdLogDescriptor(forceLog: true)
        {
            Id = logId,
            Level = LogLevel.Trace,
            Title = title,
            Content = content,

            AssemblyName = typeof(Logger).Assembly.FullName,
            ClassName = typeof(Logger).FullName,
            MethodName = nameof(ILogger.Scope)
        }, scope, _server);
        //  生成使用此Id生成新的日志作用域
        return new Logger(this, new() { ContextId = scope.ContextId, ParentId = logId });
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 构建日志作用域
    /// </summary>
    /// <returns></returns>
    private static ScopeDescriptor BuildLogScope(Logger logger)
    {
        ScopeDescriptor scope = logger._scope ?? new ScopeDescriptor()
        {
            ContextId = RunContext.Current.ContextId,
            ParentId = RunContext.Current.ParentSpanId,
        };
        return scope;
    }
    #endregion
}
