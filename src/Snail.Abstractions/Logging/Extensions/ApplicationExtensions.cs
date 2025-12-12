using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Enumerations;
using Snail.Abstractions.Logging.Interfaces;

namespace Snail.Abstractions.Logging.Extensions;

/// <summary>
/// <see cref="Logging"/>针对<see cref="IApplication"/>的扩展方法
/// </summary>
public static class ApplicationExtensions
{
    extension(IApplication app)
    {
        /// <summary>
        /// 添加日志服务
        /// <para>1、进行日志相关服务组件检测，如确保<see cref="DIKEY_FileLogger"/>组件存在</para>
        /// </summary>
        /// <returns></returns>
        public IApplication AddLogServices()
        {
            //  服务注册完成后，确保【文件】日志组件存在，否则会影响某些组件功能
            app.OnRegistered += () =>
            {
                app.ResolveRequired<ILogProvider>();
                app.ResolveRequired<ILogProvider>(key: DIKEY_FileLogger);
            };

            return app;
        }

        /// <summary>
        /// 将日志写入到本地文件中
        /// <para>1、使用<see cref="DIKEY_FileLogger"/>为Key的<see cref="ILogProvider"/>组件完成文件日志写入</para>
        /// <para>2、不会进行日志等级有效性验证，推荐用于一些核心组件的兜底，如网络日志写入报错时，使用本地日志做兜底</para>
        /// <para>3、推荐使用前，使用<see cref="AddLogServices(IApplication)"/>添加日志服务</para>
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public bool LogFile(LogDescriptor log)
        {
            var provider = app.ResolveRequired<ILogProvider>(key: DIKEY_FileLogger);
            return provider.Log(log, scope: null, server: null);
        }
        /// <summary>
        /// 将错误日志写入文本文件
        /// <para>1、使用<see cref="DIKEY_FileLogger"/>为Key的<see cref="ILogProvider"/>组件完成文件日志写入</para>
        /// <para>2、推荐用于一些核心组件的兜底，如网络日志写入报错时，使用本地日志做兜底</para>
        /// <para>3、推荐使用前，使用<see cref="AddLogServices(IApplication)"/>添加日志服务</para>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public bool LogErrorFile(string title, string? content, Exception? ex = null)
        {
            LogDescriptor log = new LogDescriptor()
            {
                Level = LogLevel.Error,
                Title = title,
                LogTag = "",
                Content = content,
                AssemblyName = typeof(ApplicationExtensions).Assembly.FullName,
                ClassName = typeof(ApplicationExtensions).FullName,
                MethodName = nameof(LogErrorFile),
                Exception = ex,
            };
            return app.LogFile(log);
        }
    }
}