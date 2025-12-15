using log4net.Config;
using Snail.Abstractions.Identity.Interfaces;
using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting.Enumerations;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.Common.Utils;
using Snail.Utilities.Xml.Utils;
using System.Text;
using System.Xml;

namespace Snail.Logger.Utils;

/// <summary>
/// Log4Net助手类
/// </summary>
internal static class Log4NetHelper
{
    #region 属性变量
    /// <summary>
    /// 日志行前缀
    /// </summary>
    private const char PREFIX_EmptyTab = '\t';
    /// <summary>
    /// 字符串：日志Id属性名
    /// </summary>
    private const string STR_IdProperty = nameof(IIdentity.Id);

    /// <summary>
    /// 日志描述器类型
    /// </summary>
    private static readonly Type _logDescriptorType = typeof(LogDescriptor);
    /// <summary>
    /// 日志锁
    /// </summary>
    private static readonly object _lock = new object();

    /// <summary>
    /// log4net是否已经初始化配置过了
    /// </summary>
    private static bool _isConfigured = false;
    #endregion

    #region 公共方法
    /// <summary>
    /// 初始化配置
    /// </summary>
    /// <param name="app"></param>
    public static void InitLogConfiguration(IApplication app)
    {
        if (_isConfigured == false)
        {
            lock (_lock)
            {
                RunIf(_isConfigured == false, ConfigLog4Net, app);
                RunIf(_isConfigured == false, ConfigAutoClearLogFile, app);
                _isConfigured = true;
            }
        }
    }

    /// <summary>
    /// 构建日志消息字符串
    /// </summary>
    /// <param name="descriptor"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    public static string BuildLogMessage(LogDescriptor descriptor, ScopeDescriptor? scope)
    {
        StringBuilder builder = new StringBuilder().AppendLine();
        bool ignoreIdKey = descriptor is IIdentity;
        //  固定组装信息
        {
            //  日志标题；日志id、作用域等信息
            builder.Append(PREFIX_EmptyTab).AppendLine(descriptor.Title);
            if (ignoreIdKey == true)
            {
                builder.Append(PREFIX_EmptyTab).AppendLine($"日志Id：{((IIdentity)descriptor).Id}");
            }
            if (scope != null)
            {
                builder.Append(PREFIX_EmptyTab).AppendLine($"操作Id：{scope.ContextId}；父级Id：{scope.ParentId ?? "null"}");
            }
            //  日志标签
            if (descriptor.LogTag != null)
            {
                builder.Append(PREFIX_EmptyTab)
                       .AppendLine($"日志标签：{descriptor.LogTag}");
            }
            //  执行方法、程序即等信息
            builder.Append(PREFIX_EmptyTab).AppendLine($"方法名称：{descriptor.MethodName}")
                   .Append(PREFIX_EmptyTab).AppendLine($"类名称：{descriptor.ClassName}")
                   .Append(PREFIX_EmptyTab).AppendLine($"程序集：{descriptor.AssemblyName}");
        }
        //  日志内容信息：组装Content、扩展属性合并到一个
        {
            List<string> extends = new List<string>();
            Type descriptorType = descriptor.GetType();
            //      非日志描述器基类，梳理扩展属性
            if (descriptorType != _logDescriptorType)
            {
                foreach (PropertyInfo pi in TypeHelper.GetProperties(descriptorType))
                {
                    //  忽略日志基类下的属性，忽略【日志主键Id】属性；忽略空值属性
                    if (pi.DeclaringType == _logDescriptorType || (ignoreIdKey && pi.Name == STR_IdProperty))
                    {
                        continue;
                    }
                    object? value = pi.GetValue(descriptor);
                    if (value == null)
                    {
                        continue;
                    }
                    //  值类型和字符串直接取；其他引用类型走json
                    if (pi.PropertyType.IsValueType == true || pi.PropertyType.IsString())
                    {
                        extends.Add($"{pi.Name}：{value}");
                    }
                    else
                    {
                        extends.Add($"{pi.Name}：{value.AsJson()}");
                    }
                }
            }
            //  组装Content内容信息；合并成日志内容数据；有扩展数据，则换行缩进组装
            if (extends.Any() == true)
            {
                builder.Append(PREFIX_EmptyTab).AppendLine("日志内容：");
                if (descriptor.Content?.Length > 0)
                {
                    extends.Add($"Content：{descriptor.Content}");
                }
                foreach (var item in extends)
                {
                    builder.Append(PREFIX_EmptyTab).Append(PREFIX_EmptyTab)
                           .AppendLine(item);
                }
            }
            else if (descriptor.Content != null)
            {
                builder.Append(PREFIX_EmptyTab)
                       .AppendLine($"日志内容：{descriptor.Content}");
            }
        }
        //  日志异常信息：有才添加，对格式做一下处理，避免和上面格式冲突
        if (descriptor.Exception != null)
        {
            var ex = descriptor.Exception.Optimize();
            builder.Append(PREFIX_EmptyTab)
                   .Append($"[{descriptor.Exception.GetType().Name}]")
                   .AppendLine("异常信息：");
            //  遍历异常信息行，做格式化
            foreach (String str in descriptor.Exception.ToString().Split(Environment.NewLine))
            {
                builder.Append(PREFIX_EmptyTab).Append(PREFIX_EmptyTab)
                       .AppendLine(str.Trim());
            }
        }

        return builder.ToString();
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 配置log4net
    /// </summary>
    /// <param name="app"></param>
    private static void ConfigLog4Net(IApplication app)
    {
        //  构建log4net配置xml文档
        XmlDocument? doc = null;
        //      从配置读取数据；仅读取默认工作空间下配置
        app.UseSetting(isProject: false, rsCode: "log4net", (workspace, _, code, type, content) =>
        {
            if (workspace != "Default") return;
            ThrowIfFalse(type == SettingType.File, $"Log4Net读取配置时，仅支持File类型，当前为：{type.ToString()}");
            RunResult<XmlDocument> rt = RunIf(workspace == "Default", XmlHelper.Load, content);
            if (rt.Exception != null)
            {
                string msg = $"读取log4net配置发生异常，Workspace:{workspace}，file:{content}";
                throw new ApplicationException(msg, rt.Exception);
            }
            doc = rt;
        });
        //      走默认配置；必须确保不报错，否则会出问题
        if (doc == null)
        {
            /** 默认日志管理器；级别上多一些没问题，但文件不用创建太多，合并一些日志出来
             *  1、默认规则
             *      Trace                   追踪级别日志，记录到【Debug】文件下
             *      Debug                   调试级别日志，记录到【Debug】文件下
             *      Info                    信息级别日志，记录到【Info】文件下
             *      Warn                    警告级别日志，记录到【Warn】文件下
             *      Error                   错误级别日志，记录到【Error】文件下
             *      System                  系统级别日志，记录到【System】文件下
             *  2、合并后规则-------------------------------后期再合并，现在先保持现状
             *      Trace、Debug、Info      合并记录到【Trace+Debug+Info】文件下
             *      Warn、Error             合并记录到【Warn+Error】文件下
             *      System                  合并记录到【System】文件下
             */
            doc = new XmlDocument();
            string configuration = """
                <?xml version="1.0" encoding="utf-8"?>
                <!--log4net配置文件：https://logging.apache.org/log4net/release/manual/configuration.html-->
                <log4net>
                	<!--日志级别字符串	ALL	DEBUG	INFO	WARN	ERROR	FATAL	None-->
                	<!--日志记录器：root为根；可继承顶级logger配置节点-->
                	<root>
                		<level value="ALL" />
                		<appender-ref ref="Debug_Appender" />
                		<appender-ref ref="Info_Appender" />
                		<appender-ref ref="Warn_Appender" />
                		<appender-ref ref="Error_Appender" />
                    </root>
                	<!--自定义日志记录器：在和root级别有重合；但文件名上需要区分时，避免同级别日志重复记录，启用additivity-->
                	<!--	Trace级别日志记录器-->
                	<logger name="Trace" additivity="false">
                		<level value="DEBUG" />
                		<appender-ref ref="Trace_Appender" />
                	</logger>
                	<!--	System级别日志记录器-->
                	<logger name="System" additivity="false">
                		<level value="FATAL" />
                		<appender-ref ref="System_Appender" />
                	</logger>
                	<!--日志存储相关配置，关联联动 appender-ref-->
                    <!--	Trace级别日志：Log4Net不带此级别，用替换 Debug，但文件名上做区分-->
                	<appender name="Trace_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="DEBUG" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'trace'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                	<!--	Debug级别日志-->
                	<appender name="Debug_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="DEBUG" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'debug'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                	<!--	Info级别日志-->
                	<appender name="Info_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="INFO" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'info'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                	<!--	Warn级别日志-->
                	<appender name="Warn_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="WARN" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'warn'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                	<!--	Error级别日志-->
                	<appender name="Error_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="ERROR" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'error'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                	<!--	System级别日志：Log4Net不带此级别日志，用替换 FATAL -->
                	<appender name="System_Appender" type="log4net.Appender.RollingFileAppender">
                		<filter type="log4net.Filter.LevelMatchFilter">
                			<levelToMatch value="FATAL" />
                		</filter>
                		<filter type="log4net.Filter.DenyAllFilter" />
                		<File value="App_Log\\.txt" />
                		<PreserveLogFileNameExtension value="true" />
                		<appendToFile value="true" />
                		<rollingStyle value="Date" />
                		<datePattern value="yyyy-MM-dd 'system'" />
                		<staticLogFileName value="false"/>
                		<layout type="log4net.Layout.PatternLayout">
                			<conversionPattern value="%date [%thread] %-5level - %message%newline" />
                		</layout>
                	</appender>
                </log4net>
                """;
            doc.LoadXml(configuration);
        }
        //  配置log4net
        XmlConfigurator.Configure(doc.DocumentElement!);
    }
    /// <summary>
    /// 配置【自动清理日志文件】功能
    /// </summary>
    /// <param name="app">应用程序对象</param>
    private static void ConfigAutoClearLogFile(IApplication app)
    {
        //  日志保留天数，<=0 时，则忽略掉不自动清理
        int logStoreDays = app.LogStoreDays;
        if (logStoreDays <= 0)
        {
            return;
        }
        //  定时器，每1小时执行一次
        TimerHelper.Start(TimeSpan.FromHours(1), () =>
        {
            DateTime nowDate = DateTime.Now;
            //  找到目录下的日志文件txt；
            string rootDirectory = Path.Combine(app.RootDirectory, "App_Log");
            string[] files = Directory.GetFiles(rootDirectory, "*.txt");
            if (files?.Any() != true)
            {
                return;
            }
            //  遍历做清理：文件大小为0的文件；超过配置天数的日志文件直接清理掉
            foreach (string file in files)
            {
                FileInfo fi = new(file);
                int tmpDay = nowDate.Subtract(fi.CreationTime).Days;
                RunIf(tmpDay >= logStoreDays || (fi.Length == 0 && tmpDay >= 3) == true, fi.Delete);
            }
        }, runRightNow: false);
    }
    #endregion
}
