using System.Diagnostics;
using Snail.Abstractions.Identity;
using Snail.Abstractions.Logging;
using Snail.Abstractions.Logging.Enumerations;
using Snail.Abstractions.Web.Attributes;
using Snail.Abstractions.Web.Delegates;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Utils;
using Snail.Web.DataModels;

namespace Snail.Web.Components
{
    /// <summary>
    /// HTTP请求的日志中间件
    /// </summary>
    [Component<IHttpMiddleware>(Lifetime = LifetimeType.Singleton, Key = MIDDLEWARE_Logging)]
    public class LogMiddleware : IHttpMiddleware
    {
        #region 属性变量
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger { private init; get; }
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

        #region IHttpMiddleware
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="request">http请求</param>
        /// <param name="server">请求服务器</param>
        /// <param name="next">下一个操作</param>
        /// <returns></returns>
        async Task<HttpResponseMessage> IHttpMiddleware.Send(HttpRequestMessage request, IServerOptions server, HttpDelegate next)
        {
            //  初始化，记录发送日志：查询日志记录标签，null走默认值
            DiagnosticsHelper.GetEntryMethod(typeof(LogMiddleware), out HttpLogAttribute? attr);
            attr ??= new HttpLogAttribute();
            //  记录请求日志
            LogSend(attr, request, server);
            //  发送请求，记录请求耗时、响应结果、拦截错误日志
            Stopwatch? sw = attr.Performance == true ? Stopwatch.StartNew() : null;
            Exception? tmpEx = null;
            HttpResponseMessage? response = null;
            try
            {
                response = await next(request, server);
                return response;
            }
            catch (Exception ex)
            {
                tmpEx = ex;
                throw;
            }
            finally
            {
                sw?.Stop();
                LogResponse(attr, request, response, tmpEx, sw?.ElapsedMilliseconds);
            }
        }
        #endregion

        #region 继承方法
        /// <summary>
        /// 构建发送日志
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="server"></param>
        /// <param name="request">请求实体</param>
        protected virtual void LogSend(HttpLogAttribute attr, HttpRequestMessage request, IServerOptions server)
        {
            //  若启用追踪日志，则强制分配日志id
            string? logId = _starTrace == true ? IdGenerator.NewId("HttpLog") : null;
            //  cookie附带数据：是否记录请求数据、父级操作Id值
            if (attr.Send == true)
            {
                request.Headers.Add("Cookie", $"_LOGSENDDATA_={bool.TrueString}");
            }
            if (_starTrace == true)
            {
                request.Headers.Add("Cookie", $"{CONTEXT_ParentActionId}={logId}");
            }
            //  记录日志：将日志id记录上，启用追踪则为强制日志
            Logger.Log(new SendLogDescriptor(_starTrace)
            {
                Title = $"HTTP请求开始：{GetRequestUrl(request)}",
                LogTag = "HTTP",
                Content = attr.Send == true ? BuildString(request.Content) : null,
                Level = LogLevel.Trace,
                AssemblyName = GetType().Assembly.FullName,
                ClassName = GetType().FullName,
                MethodName = nameof(LogSend),
                Exception = null,

                Id = logId ?? string.Empty,
                ServerOptions = server?.ToString(),
                HttpMethod = request.Method.ToString(),
                Headers = request.Headers?.ToDictionary(item => item.Key, item => item.Value?.AsString(';')),
            });
        }
        /// <summary>
        /// 记录请求结果日志
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="request">请求实体</param>
        /// <param name="response">响应结果实体，为null表示请求发生错误</param>
        /// <param name="ex">响应异常信息；为null表示请求成功</param>
        /// <param name="performance">若开启了性能日志，则传入具体耗时时间；单位毫秒</param>
        protected virtual void LogResponse(HttpLogAttribute attr, HttpRequestMessage request, HttpResponseMessage? response, Exception? ex, long? performance)
        {
            //  判断是否需要记录日志：报错时先强制记录；未报错时，不记录请求结果和性能日志时，不记录
            if (ex == null && attr.Response != true && attr.Performance != true)
            {
                return;
            }
            //  错误时，记录成错误日志；否则跟踪日志
            ResponseLogDescriptor descriptor = new()
            {
                Title = ex == null
                        ? $"HTTP请求结束：{GetRequestUrl(request)}"
                        : $"HTTP请求异常：{GetRequestUrl(request)}",
                LogTag = ex == null ? "Result" : null,
                Content = attr.Response == true ? BuildString(response?.Content) : null,
                Level = ex == null ? LogLevel.Trace : LogLevel.Error,
                AssemblyName = GetType().Assembly.FullName,
                ClassName = GetType().FullName,
                MethodName = nameof(LogResponse),
                Exception = ex,

                Headers = response?.Headers?.ToDictionary(item => item.Key, item => item.Value?.AsString(';')),
                Performance = performance,
            };

            Logger.Log(descriptor);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 构建字符串
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string? BuildString(HttpContent? content)
        {
            //  若获取字符串失败，则将失败原因放进来；后续处理一下，若结果为二进制或者文件数据，怎么记录
            if (content == null)
            {
                return null;
            }
            //  读取数据，拦截异常
            try
            {
                return content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                return $"构建HttpContent字符串失败：{ex}";
            }
        }

        /// <summary>
        /// 获取请求URL地址
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private static string GetRequestUrl(HttpRequestMessage request)
        {
            ThrowIfNull(request);
            if (request.RequestUri == null)
            {
                return string.Empty;
            }
            else
            {
                return request.RequestUri.IsAbsoluteUri == true
                    ? request.RequestUri.PathAndQuery.TrimStart('/')
                    : request.RequestUri.ToString().TrimStart('/');
            }
        }
        #endregion
    }
}
