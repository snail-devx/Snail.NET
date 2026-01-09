using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Snail.Abstractions.ErrorCode;
using Snail.Abstractions.ErrorCode.DataModels;
using Snail.Abstractions.ErrorCode.Exceptions;
using Snail.Abstractions.ErrorCode.Extensions;
using Snail.Abstractions.ErrorCode.Interfaces;
using Snail.Abstractions.Logging.Attributes;
using Snail.Abstractions.Logging.DataModels;
using Snail.Abstractions.Logging.Extensions;
using Snail.Abstractions.Setting.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.WebApp.Attributes;
using Snail.WebApp.DataModels;
using Snail.WebApp.Enumerations;
using Snail.WebApp.Extensions;
using Snail.WebApp.Interfaces;
using System.Collections.ObjectModel;
using ILogger = Snail.Abstractions.Logging.ILogger;
using LogLevel = Snail.Abstractions.Logging.Enumerations.LogLevel;

namespace Snail.WebApp.Components;

/// <summary>
/// API请求动作 基础过滤器
/// <para>集成常用api响应相关功能，并支持外部di覆盖重写实现更精细化逻辑</para>
/// <para>基于以下特性标签实现具体功能；特性标签查找路径：Action->控制器->依赖注入（Key为null）</para>
/// <para>1、基于<see cref="LogAttribute"/>配置拦截api，进行请求、响应相关日志记录</para>
/// <para>2、基于<see cref="ErrorAttribute"/>配置，捕捉api执行过程中的错误信息，根据配置进行错误编码包装</para>
/// <para>3、基于<see cref="ContentAttribute"/>配置，验证提交数据是否符合格式，如只支持json格式数据提交</para>
/// <para>4、基于<see cref="AuthAttribute"/>配置，进行api鉴权，验证Token信息</para>
/// <para>5、基于<see cref="ResponseAttribute"/>配置，拦截api响应数据分析返回数据是否需要进行自定义JSON序列化处理</para>
/// <para>6、基于<see cref="PerformanceAttribute"/>配置，进行api响应性能追踪，记录到请求结束日志中</para>
/// </summary>
public abstract class ActionBaseFilter : IAsyncActionFilter
{
    #region 属性变量
    /// <summary>
    /// 当前应用实例
    /// </summary>
    [Inject]
    public required IApplication App { init; get; }
    /// <summary>
    /// 错误编码管理器
    /// </summary>
    [Inject]
    public required IErrorCodeManager ErrorManager { init; get; }
    /// <summary>
    /// 日志记录器
    /// </summary>
    [Logger]
    public required ILogger Logger { init; get; }

    /// <summary>
    /// JSON序列化配置字典
    /// <para>将常用类型固化实例，避免频繁创建</para>
    /// </summary>
    protected readonly ReadOnlyDictionary<JsonResolverType, IContractResolver> JsonResolverMap = new(
        new Dictionary<JsonResolverType, IContractResolver>{
            //  默认：属性名称保持不变，不做处理
            { JsonResolverType.Default, new DefaultContractResolver() },
            //  驼峰：首字母小写
            { JsonResolverType.CamelCase, new CamelCasePropertyNamesContractResolver () },
            //  全小写
            { JsonResolverType.LowerCase, new LowercaseContractResolver()},
        }
    );
    #endregion

    #region 构造方法
    #endregion

    #region IAsyncActionFilter
    /// <summary>
    /// 异步执行具体的API
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="next"></param>
    /// <returns></returns>
    async Task IAsyncActionFilter.OnActionExecutionAsync(ActionExecutingContext request, ActionExecutionDelegate next)
    {
        //  准备工作
        DateTime startDate = DateTime.Now;
        ActionExecutedContext? response = null;
        Exception? tmpEx = null;
        //      取到需要使用到的特性标签
        LogAttribute? logAttr = GetAtrtibute<LogAttribute>(request);
        ContentAttribute? contentAttr = GetAtrtibute<ContentAttribute>(request);
        AuthAttribute? authAttr = GetAtrtibute<AuthAttribute>(request);
        ErrorAttribute? errorAttr = GetAtrtibute<ErrorAttribute>(request);
        ResponseAttribute? responseAttr = GetAtrtibute<ResponseAttribute>(request);
        PerformanceAttribute? perfAttr = GetAtrtibute<PerformanceAttribute>(request);
        //  请求基础验证等处理后执行next完成具体Action逻辑：记录开始日志、提交内容验证、令牌权限验证、、、
        try
        {
            await RunAttributeTask([
                new(logAttr, () => CaptureLog(request,logAttr!)),
                new(contentAttr, () => CaptureContent(request, contentAttr!)),
                new(authAttr, () => CaptureAuth(request, authAttr!))
            ]);
            response = await RunNext(next);
        }
        //  发生错误时，未启用错误处理时，直接抛出异常
        catch (Exception ex)
        {
            if (errorAttr?.Disabled != false)
            {
                throw;
            }
            tmpEx = ex;
        }
        //  请求完成后，进行异常、响应结果等处理
        finally
        {
            DateTime endDate = DateTime.Now;
            tmpEx ??= response?.Exception;
            await RunAttributeTask([
                //  发生异常，进行异常捕捉：有异常时才捕获
                new(errorAttr,  () => CaptureError(request, response, errorAttr!, tmpEx!), () => tmpEx != null),
                //  响应结果处理：未发生异常，或者异常被上一步捕捉了
                new(
                    responseAttr,
                    () => CaptureResponse(request,response,responseAttr!),
                    () => response == null ? request.Result != null : response.Exception == null
                ),
                //  记录请求结束日志
                new(logAttr, () => CaptureLog(request, response, logAttr!, tmpEx, startDate)),
                //  计算请求耗时，添加性能追踪
                new(perfAttr, () => CapturePerformance(request, perfAttr!, startDate)),
            ]);
        }
    }
    #endregion

    #region 继承方法

    #region 通用方法
    /// <summary>
    /// 获取api动作标注的特性标签
    /// <para>获取优先级：Action->控制器->依赖注入（Key为null）</para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="request">请求上下文</param>
    /// <returns></returns>
    protected virtual T? GetAtrtibute<T>(in ActionExecutingContext request) where T : Attribute
    {
        T? attr = null;
        //  暂时先支持【Controller】控制器模式API，后续支持 miniapi模式；
        switch (request.ActionDescriptor)
        {
            //  控制器的aciton：方法上无标签时，则尝试从控制器class中查找，会自动查找父级路径上的特性标签
            case ControllerActionDescriptor ca:
                attr = ca.MethodInfo.GetCustomAttribute<T>()
                    ?? request.Controller.GetType().GetCustomAttribute<T>();
                break;
            //  默认情况不做分析处理
            default:
                break;
        }
        //  兜底从di中分析
        return attr ?? App.Resolve<T>();
    }
    /// <summary>
    /// 运行【API动作标签任务】
    /// <para>执行<see cref="ActionAttributeTask{T}.Task"/>时，若返回非null，自动执行await，然后再执行下一任务</para>
    /// </summary>
    /// <param name="tasks"></param>
    /// <returns></returns>
    protected virtual async Task RunAttributeTask(ActionAttributeTask<IActionAttribute>[] tasks)
    {
        //  遍历执行，任务执行条件：启用了此标签时；若有断言条件，则断言条件执行返回true
        foreach (var (attr, func, predicate) in tasks)
        {
            if (attr?.Disabled == false && predicate?.Invoke() != false)
            {
                Task? task = func();
                if (task != null)
                {
                    await task;
                }
            }
        }
    }

    /// <summary>
    /// 分析请求Action名称
    /// </summary>
    /// <para>1、分析<see cref="ActionAttribute"/>特性标签取值</para>
    /// <returns></returns>
    protected virtual string AnalysisActionName(in ActionExecutingContext request)
    {
        string? actionName = GetAtrtibute<ActionAttribute>(request)?.Name;
        //  为空的时候，基于分析控制器的Action名称
        if (string.IsNullOrEmpty(actionName) == true)
        {
            switch (request.ActionDescriptor)
            {
                //  控制器的aciton
                case ControllerActionDescriptor ca:
                    actionName = $"{request.Controller.GetType().Name}/{ca.ActionName}";
                    break;
                //  其他情况，不支持返回未知
                default:
                    actionName = "UnknownAction";
                    break;
            }
        }
        return actionName;
    }
    /// <summary>
    /// 分析请求Action标签
    /// <para>1、分析<see cref="ActionAttribute"/>特性标签取值</para>
    /// <para>2、无特性标签时，直接返回 API</para>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected virtual string AnalysisActionTag(in ActionExecutingContext request)
        => GetAtrtibute<ActionAttribute>(request)?.Tag ?? "API";

    /// <summary>
    /// 执行过滤器的 next 动作方法，完成实际业务api调用
    /// <para>在内置的逻辑出来完成执行此方法，外部可重写完成自定义逻辑处理</para>
    /// </summary>
    /// <param name="next"></param>
    protected virtual Task<ActionExecutedContext> RunNext(in ActionExecutionDelegate next) => next();

    /// <summary>
    /// 设置响应结果
    /// <para>1、<paramref name="response"/>非null时，设置给<see cref="ActionExecutedContext.Result"/>，并强制将<see cref="ActionExecutedContext.Exception"/>置null</para>
    /// <para>2、<paramref name="response"/>为null时，设置给<see cref="ActionExecutingContext.Result"/></para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="request">请求上下文</param>
    /// <param name="response">响应结果上下文；未执行具体api操作时为null</param>
    /// <param name="result"></param>
    protected virtual void SetResponse<T>(ActionExecutingContext request, ActionExecutedContext? response, T? result) where T : IActionResult
    {
        if (response != null)
        {
            response.Exception = null;
            response.Result = result;
        }
        else
        {
            request.Result = result;
        }
    }
    #endregion

    #region LogAttribute：日志处理
    /// <summary>
    /// 记录API开始日志
    /// <para><see cref="LogAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="attr"></param>
    /// <returns></returns>
    protected virtual async Task? CaptureLog(ActionExecutingContext request, LogAttribute attr)
    {
        //  分析提交的内容数据
        string? content = await BuildPostContent(request.HttpContext.Request, attr);
        //  构建日志详细，并记录为Trace级别日志
        LogDescriptor descriptor = new ActionExecutingLogDescriptor()
        {
            Title = $"接收API请求：{AnalysisActionName(request)}",
            LogTag = AnalysisActionTag(request),
            Content = content,
            Level = LogLevel.Trace,
            AssemblyName = typeof(ActionBaseFilter).Assembly.FullName,
            ClassName = typeof(ActionBaseFilter).FullName,
            MethodName = nameof(IAsyncActionFilter.OnActionExecutionAsync),
            Exception = null,

            HttpMethod = request.HttpContext.Request.Method,
            //"Accept-Language": ["zh-CN,zh;q=0.9"]
            Headers = attr.Header == true
                ? request.HttpContext.Request.Headers.ToDictionary(item => item.Key, item => item.Value.ToString())
                : null,
            RequestURL = request.HttpContext.Request.AbsoluteUrl(),
        };
        Logger.Log(descriptor);
    }
    /// <summary>
    /// 构建Post提交数据内容
    /// <para>用于做日志记录使用</para>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="attr"></param>
    /// <returns></returns>
    protected virtual async Task<string?> BuildPostContent(HttpRequest request, LogAttribute attr)
    {
        //  若已经标记记录了数据，则先忽略
        if (bool.TrueString.IsEqual(request.Headers[KEY_RecordData], ignoreCase: true))
        {
            return "发送方已记录，不再重复记录";
        }
        //  分析请求格式：JSON结构特定处理，否则取Form提交数据；其他情况无效
        if (attr.Content == true)
        {
            if (request.HasJsonContentType() == true)
            {
                try
                {
                    return await request.ReadStringBody();
                }
                catch (Exception ex)
                {
                    return $"分析JSON格式数据失败:{ex}";
                }
            }
            else if (request.HasFormContentType == true && request.Form.Count > 0)
            {
                Dictionary<string, string?> dict = new();
                foreach (var kv in request.Form)
                {
                    //  采用特殊Key：key为null；或者取post参数，如果带有“<>”，会被认为是攻击性代码，
                    try { dict[kv.Key ?? "NULL^KEY"] = kv.Value; }
                    catch (Exception ex) { dict[kv.Key ?? "NULL^KEY"] = ex.Message; }
                }
                return dict.AsJson();
            }
        }
        return null;
    }

    /// <summary>
    /// 记录API结束日志
    /// <para><see cref="LogAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="response">响应结果上下文；未执行具体api操作时为null</param>
    /// <param name="attr">日志请求标签</param>
    /// <param name="ex">请求异常信息；发生异常时非null</param>
    /// <param name="start">请求开始时间</param>
    /// <returns></returns>
    protected virtual Task? CaptureLog(ActionExecutingContext request, ActionExecutedContext? response, LogAttribute attr, Exception? ex, DateTime start)
    {
        LogDescriptor descriptor = new ActionExecutedLogDescriptor()
        {
            Title = $"API请求结束：{AnalysisActionName(request)}",
            LogTag = "Result",
            Content = null,
            Level = ex == null ? LogLevel.Trace : LogLevel.Error,
            AssemblyName = typeof(ActionBaseFilter).Assembly.FullName,
            ClassName = typeof(ActionBaseFilter).FullName,
            MethodName = nameof(IAsyncActionFilter.OnActionExecutionAsync),
            Exception = ex,
            Performance = Convert.ToInt64(DateTime.Now.Subtract(start).TotalMilliseconds)
        };
        Logger.Log(descriptor);
        return null;
    }
    #endregion

    #region ContentAttribute：提交数据处理
    /// <summary>
    /// 验证API提交数据
    /// <para><see cref="ContentAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="attr"></param>
    /// <returns>若为异步，则返回非null Task对象</returns>
    protected virtual Task? CaptureContent(ActionExecutingContext request, ContentAttribute attr)
    {
        //  无提交数据时，不做处理；遍历请求传递的Content-Type做分析验证：指定 全部 时，不做处理
        bool needValidateContent = request.HttpContext.Request.ContentLength > 0
            && (attr.Allow & ContentType.All) != ContentType.All;
        if (needValidateContent == true)
        {
            ContentType? ct = null;
            if (request.HttpContext.Request.ContentType?.Length > 0)
            {
                ct = request.HttpContext.Request.ContentType.Split(';')
                    .Select<string, ContentType?>(str => str.Trim().ToLower() switch
                    {
                        "application/json" => ContentType.Json,
                        "application/x-www-form-urlencoded" => ContentType.FormUrl,
                        _ => null
                    })
                    .FirstOrDefault(type => type != null);
            }
            ct ??= ContentType.All;
            //  验证合法性
            if ((attr.Allow & ct) != ct)
            {
                string msg = $"不支持的Content-Type值：{request.HttpContext.Request.ContentType}";
                throw new NotSupportedException(msg);
            }
        }
        return null;
    }
    #endregion

    #region AuthAttribute：鉴权处理
    /// <summary>
    /// 处理API鉴权验证
    /// <para><see cref="AuthAttribute.Disabled"/>为false时才会调用此方法</para>
    /// <para>抽象方法，需要外部做自己的实现</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="attr"></param>
    /// <returns>若为异步，则返回非null Task对象</returns>
    protected abstract Task? CaptureAuth(ActionExecutingContext request, AuthAttribute attr);
    #endregion

    #region ErrorAttribute：错误处理 
    /// <summary>
    /// 处理API响应异常
    /// <para><see cref="ErrorAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="response">响应结果上下文；未执行具体api操作时为null</param>
    /// <param name="attr"></param>
    /// <param name="ex">错误异常对象</param>
    /// <remarks>若启用了日志，外部会自动将性能信息写入日志中；不用在此方法中处理</remarks>
    /// <returns>若为异步，则返回非null Task对象</returns>
    protected virtual Task? CaptureError(ActionExecutingContext request, ActionExecutedContext? response, ErrorAttribute attr, Exception ex)
    {
        IErrorCode? error = null;
        if (ex is ErrorCodeException ece)
        {
            error = ece.ErrorCode;
        }
        else if (string.IsNullOrEmpty(attr.ErrorCode) == false)
        {
            error = ErrorManager.GetRequired(attr.ErrorCode);
        }
        error ??= new ErrorCodeDescriptor("-1", "Unknown Error!");
        //  将异常信息写入返回值中：方便调试，返回详细异常信息
        error = new ErrorCodeDetailDescriptor(error)
        {
            Type = ex.GetType().Name,
            Detail = App.IsProduction ? ex.Message : ex.ToString()
        };
        SetResponse(request, response, new ObjectResult(error));
        return null;
    }
    #endregion

    #region ResponseAttribute：响应结果处理
    /// <summary>
    /// 处理API响应结果
    /// <para><see cref="ResponseAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="response">响应结果上下文；未执行具体api操作时为null</param>
    /// <param name="attr"></param>
    /// <returns>若为异步，则返回非null Task对象</returns>
    protected virtual Task? CaptureResponse(ActionExecutingContext request, ActionExecutedContext? response, ResponseAttribute attr)
    {
        //  得到响应结果，暂时处理 JsonResult和ObjectResult；其他情况的Result外部自行做处理
        IActionResult? actionResult = response == null ? request.Result : response.Result;
        object? result;
        switch (actionResult)
        {
            //  支持JSON和Object即可，得到具体的响应结果
            //      JsonResult
            case JsonResult jr:
                result = jr.Value;
                break;
            //      ObjectResult
            case ObjectResult or:
                result = or.Value;
                break;
            //  下面这些情况，不做支持，直接返回即可
            //      为null或者void对象，则不用处理
            case null:
            case EmptyResult _:
                return null;
            //      其他情况，Result外部自行做处理，不清楚具体结构和处理逻辑
            default:
                string msg = $"仅支持JsonResult/ObjectResult结果值处理；当前类型：{actionResult.GetType().Name}";
                Logger.Warn("CaptureResponse：不支持的ActionResult", msg);
                return null;
        }
        //  进行响应结果序列化：配置自定义序列化器时，基于di进行反射构建
        if (result != null)
        {
            JsonResolverMap.TryGetValue(attr.JsonResolver, out IContractResolver? resolver);
            if (resolver == null && attr.JsonResolver == JsonResolverType.Custom)
            {
                resolver = App.Resolve<IContractResolver>(attr.JsonCustomResolver);
                if (resolver == null)
                {
                    string content = $"Resolve返回null。key:{attr.JsonCustomResolver ?? STR_Null};from:{typeof(IContractResolver).FullName}";
                    Logger.Warn("CaptureResponse：构建JSON序列化解析器失败", content);
                }
            }
            if (resolver != null)
            {
                JsonSerializerSettings jsonSetting = new JsonSerializerSettings()
                {
                    ContractResolver = resolver,
                    NullValueHandling = attr.JsonIgnoreNullValue == true ? NullValueHandling.Ignore : NullValueHandling.Include,
                };
                SetResponse(request, response, new JsonResult(result, jsonSetting));
            }
        }
        return null;
    }
    #endregion

    #region PerformanceAttribute：性能追踪
    /// <summary>
    /// 追踪API响应性能
    /// <para><see cref="PerformanceAttribute.Disabled"/>为false时才会调用此方法</para>
    /// </summary>
    /// <param name="request">请求上下文</param>
    /// <param name="attr"></param>
    /// <param name="start">api请求开始时间</param>
    /// <remarks>若启用了日志，外部会自动将性能信息写入日志中；不用在此方法中处理</remarks>
    /// <returns>若为异步，则返回非null Task对象</returns>
    protected virtual Task? CapturePerformance(ActionExecutingContext request, PerformanceAttribute attr, DateTime start)
    {
        //  默认什么都不做，启用日志时会自动将性能信息写入日志中；若需要进行性能大户监测，可以单独在这里记录到数据库
        return null;
    }
    #endregion

    #endregion

    #region 私有方法

    #endregion
}