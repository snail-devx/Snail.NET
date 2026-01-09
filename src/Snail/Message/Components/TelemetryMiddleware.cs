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
using System.Diagnostics;

namespace Snail.Message.Components;

/// <summary>
/// 消息遥测中间件
/// <para>1、拦截消息处理，记录消息处理日志</para>
/// <para>2、发送请求时，附带标准遥测相关数据</para>
/// </summary>
[Component<IMessageMiddleware>(Key = MIDDLEWARE_Telemetry)]
public class TelemetryMiddleware : IMessageMiddleware
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
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app"></param>
    public TelemetryMiddleware(IApplication app)
    {
        ThrowIfNull(app);
        Logger = app.ResolveRequired<ILogger>();
        IdGenerator = app.ResolveRequired<IIdGenerator>();
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
    async Task<bool> ISendMiddleware.Send(MessageType type, MessageDescriptor message, IMessageOptions options, IServerOptions server, SendDelegate next)
    {
        //  初始化遥测追踪数据，记录日志，并将【KEY_RecordData】标记传入到消息上下文中
        {
            string parentSpanId = IdGenerator.NewId("MessageLog");
            message.Context ??= new Dictionary<string, string>();
            message.Context.Set(KEY_RecordData, STR_True);
            InitializeSend(message, RunContext.Current, parentSpanId);
            Logger.Log(new MessageSendLogDescriptor(isForce: true)
            {
                Title = $"发送{type}消息：{message.Name}",
                LogTag = type.ToString(),
                Content = message.AsJson(),
                Level = LogLevel.Trace,
                AssemblyName = typeof(TelemetryMiddleware).Assembly.FullName,
                ClassName = typeof(TelemetryMiddleware).FullName,
                MethodName = nameof(ISendMiddleware.Send),
                Exception = null,

                Id = parentSpanId,
                ServerOptions = server.AsJson(),
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
    async Task<bool> IReceiveMiddleware.Receive(MessageType type, MessageDescriptor message, IReceiveOptions options, IServerOptions server, ReceiveDelegate next)
    {
        Exception? tmpEx = null;
        //  初始化遥测追踪数据，记录消息日志
        {
            InitializeReceive(message, RunContext.Current);
            //  若发送方标记了已经记录了消息数据，则这里不再重复记录了
            bool logData = true;
            if (message.Context?.Remove(KEY_RecordData, out string? tmpString) == true)
            {
                logData = bool.TrueString.IsEqual(tmpString, ignoreCase: true) != true;
            }
            Logger.Log(new LogDescriptor(forceLog: true)
            {
                Level = LogLevel.Trace,
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
                AssemblyName = typeof(TelemetryMiddleware).Assembly.FullName,
                ClassName = typeof(TelemetryMiddleware).FullName,
                MethodName = nameof(IReceiveMiddleware.Receive),
            });
        }
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
                Level = tmpEx == null ? LogLevel.Trace : LogLevel.Error,
                Title = tmpEx == null
                    ? $"完成{type}消息处理：{message.Name}"
                    : $"接收{type}消息异常：{message?.Name}",
                LogTag = "Result",
                Content = null,
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

    #region 继承方法
    /// <summary>
    /// 【发送消息】初始化遥测追踪信息
    /// </summary>
    /// <param name="message">要发送的消息数据</param>
    /// <param name="context">当前运行时上下文</param>
    /// <param name="parentSpanId">当前父级操作Id</param>
    protected virtual void InitializeSend(in MessageDescriptor message, in RunContext context, in string parentSpanId)
    {
        message.Context ??= new Dictionary<string, string>();
        //  在这里构建标准化的追踪参数；先写入 X-Trace-Id header中
        message.Context[CONTEXT_TraceId] = context.TraceId;
        message.Context[CONTEXT_ParentSpanId] = parentSpanId;
        //  后期支持w3c的 TraceContext 标准逻辑
    }
    /// <summary>
    /// 【接收消息】初始化遥测追踪信息
    /// </summary>
    /// <param name="message">接收到的消息数据</param>
    /// <param name="context">全新的运行时上下文</param>
    protected virtual void InitializeReceive(in MessageDescriptor message, in RunContext context)
    {
        //  分析消息中的 标准化参数，构建 trace-id和parent-span-id
        if (message.Context?.Count > 0)
        {
            message.Context.Remove(CONTEXT_TraceId, out string? traceId);
            message.Context.Remove(CONTEXT_ParentSpanId, out string? parentSpanId);
            context.InitTelemetry(traceId, parentSpanId);
        }
    }
    #endregion
}
