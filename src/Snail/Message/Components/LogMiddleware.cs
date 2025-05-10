using System.Diagnostics;
using Snail.Abstractions.Identity;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Enumerations;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Message.DataModels;
using Snail.Abstractions.Message.Delegates;
using Snail.Abstractions.Message.Enumerations;
using Snail.Abstractions.Message.Interfaces;
using Snail.Abstractions.Web.Interfaces;
using Snail.Message.DataModels;
using Snail.Utilities.Common.Extensions;

namespace Snail.Message.Components
{
    /// <summary>
    /// 日志中间件
    /// </summary>
    [Component<IMessageMiddleware>(Key = MIDDLEWARE_Logging, Lifetime = LifetimeType.Singleton)]
    public class LogMiddleware : IMessageMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger Logger;
        /// <summary>
        /// 主键Id生成器
        /// </summary>
        protected IIdGenerator IdGenerator { private init; get; }
        /// <summary>
        /// 是否启用日志追踪功能
        /// </summary>
        private readonly bool _starTrace;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app"></param>
        /// <param name="starTrace"></param>
        public LogMiddleware(IApplication app, bool starTrace = true)
        {
            ThrowIfNull(app);
            Logger = app.ResolveRequired<ILogger>();
            IdGenerator = app.ResolveRequired<IIdGenerator>();
            _starTrace = starTrace;
        }
        #endregion

        #region IMessageMiddleware
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="type">消息类型：如mq、pubsub</param>
        /// <param name="message">发送的消息数据</param>        
        /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
        /// <param name="server">消息服务器地址消息发送哪里</param>
        /// <param name="next">下一个消息处理委托</param>
        /// <returns></returns>
        async Task<bool> ISendMiddleware.Send(MessageType type, MessageData message, IMessageOptions options, IServerOptions server, SendDelegate next)
        {
            //  记录发送消息信息
            {
                //   Context为null，强制初始化，并加标记
                message.Context ??= new Dictionary<string, string>
                {
                    [CONTEXT_ContextIsNull] = "1"
                };
                //  上下文上，强制加上发送方已记录请求数据
                message.Context["_LOGSENDDATA_"] = "True";
                //  若启用日志追踪，则加入LogId，并强制记录日志
                string? logId = null;
                if (_starTrace == true)
                {
                    logId = IdGenerator.NewId("MessageLog");
                    message.Context[CONTEXT_ParentActionId] = logId;
                }
                //  发送日志
                Logger.Log(new MessageSendLogDescriptor(_starTrace)
                {
                    Title = $"发送{type}消息：{message.Name}",
                    LogTag = type.ToString(),
                    Content = message.AsJson(),
                    Level = LogLevel.Trace,
                    AssemblyName = GetType().Assembly.FullName,
                    ClassName = GetType().FullName,
                    MethodName = "Send",
                    Exception = null,

                    Id = logId!,
                    ServerOptions = server.ToString(),
                });
            }
            //  拦截异常，记录发送消息的错误信息
            try
            {
                bool bValue = await next.Invoke(type, message, options, server);
                return bValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"发送{type}消息异常：{message.Name}", String.Empty, ex);
                //  记录日志后，不再对外抛出异常
                //throw;
                return false;
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="type">消息类型：如mq、pubsub</param>
        /// <param name="message">发送的消息数据</param>        
        /// <param name="options">消息相关信息描述器，如消息名称、路由、队列、交换机等信息</param>
        /// <param name="server">消息服务器地址：接收的消息来自哪里</param>
        /// <param name="next">下一个消息处理委托</param>
        /// <returns></returns>
        async Task<bool> IReceiveMiddleware.Receive(MessageType type, MessageData message, IReceiveOptions options, IServerOptions server, ReceiveDelegate next)
        {
            Exception? tmpEx = null;
            //  记录消息日志：若发送方已经记录消息数据了，则接收方不再记录
            bool logData = message.Context?.ContainsKey("_LOGSENDDATA_") != true;
            Logger.Log(new LogDescriptor(forceLog: true)
            {
                Title = $"接收{type}消息：{message.Name}",
                LogTag = type.ToString(),
                Content = logData == true
                    ? message.AsJson()
                    : new
                    {
                        message.Name,
                        Data = "发送方已记录，不再重复记录",
                        message.Context
                    }.AsJson(),
                Level = LogLevel.Trace,
                AssemblyName = GetType().Assembly.FullName,
                ClassName = GetType().FullName,
                MethodName = "Receive",
            });
            //  进行消息处理；拦截异常，记录耗时时间
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                bool rt = await next(type, message, options, server);
                return rt;
            }
            catch (Exception ex)
            {
                tmpEx = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                Logger.Log(new MessageReceiveLogDescriptor()
                {
                    Title = tmpEx == null
                            ? $"完成{type}消息处理：{message.Name}"
                            : $"接收{type}消息异常：{message?.Name}",
                    LogTag = "Result",
                    Content = null,
                    Level = tmpEx == null ? LogLevel.Trace : LogLevel.Error,
                    AssemblyName = GetType().Assembly.FullName,
                    ClassName = GetType().FullName,
                    MethodName = "Receive",
                    Exception = tmpEx,

                    Performance = sw.ElapsedMilliseconds,
                    ServerOptions = server.ToString()
                });
            }
        }
        #endregion
    }
}
