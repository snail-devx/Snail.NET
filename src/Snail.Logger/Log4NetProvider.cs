using log4net;
using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.Logger.Utils;

namespace Snail.Logger
{
    /// <summary>
    /// Log4Net实现的日志提供程序 <br />
    ///     1、从Default工作空间下读取log4net配置，若不存在则采用程序内置的 <br />
    ///     2、记录日志
    /// </summary>
    [Component<ILogProvider>(Lifetime = LifetimeType.Singleton)]
    [Component<ILogProvider>(Lifetime = LifetimeType.Singleton, Key = DIKEY_FileLogger)]
    [Component<ILogProvider>(Lifetime = LifetimeType.Singleton, Key = "Log4Net")]
    public sealed class Log4NetProvider : ILogProvider
    {
        #region 属性变量

        /// <summary>
        /// 应用程序配置管理器
        /// </summary>
        private readonly IApplication _app;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        public Log4NetProvider(IApplication app)
        {
            ThrowIfNull(app);
            _app = app;
        }
        #endregion

        #region ILogger
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="descriptor">要记录的日志信息</param>
        /// <param name="scope">日志作用域描述器；用于区分日志组等情况</param>
        /// <param name="serverOptions">服务器配置选项；为null提供程序自身做默认值处理，或者报错 <br />
        ///     1、记录器为网络日志时，日志要记录到哪个服务器下，如哪个数据库服务器 <br />
        ///     2、记录器为本地日志时，采用哪个工作组下的配置，如log4net配置；此时仅<see cref="IServerOptions.Workspace"/>生效 <br />
        /// </param>
        /// <returns>记录成功；返回true</returns>
        /// <remarks>针对log4net来说,<paramref name="serverOptions"/>无任何意义，不会使用</remarks>
        bool ILogProvider.Log(LogDescriptor descriptor, ScopeDescriptor? scope, IServerOptions? serverOptions)
        {
            ThrowIfNull(descriptor);
            //  初始化日志记录器；确保只初始化一次
            Log4NetHelper.InitLogConfiguration(_app);
            //  进行日志记录
            var logger = LogManager.GetLogger(descriptor.Level.ToString());
            ThrowIfNull(logger);
            string message = Log4NetHelper.BuildLogMessage(descriptor, scope);
            switch (descriptor.Level)
            {
                //  Trace log4net无此级别，用debug替换，但LoggerName用“Trace”
                //  Trace log4net无此级别，用Fatal替换，但LoggerName用“System”
                case LogLevel.Trace: logger.Debug(message); break;
                case LogLevel.Debug: logger.Debug(message); break;
                case LogLevel.Info: logger.Info(message); break;
                case LogLevel.Warn: logger.Warn(message); break;
                case LogLevel.Error: logger.Error(message); break;
                case LogLevel.System: logger.Fatal(message); break;
                //  其他情况不支持
                default: throw new NotSupportedException($"不支持的日志等级：{descriptor.Level}");
            }
            return true;
        }
        #endregion
    }
}
