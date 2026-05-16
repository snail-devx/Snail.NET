using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.Extensions;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Web.Components;

/// <summary>
/// HTTP初始化器
/// </summary>
[Component<IInitializer<IHttpManager>>(Lifetime = LifetimeType.Transient)]
public class HttpInitializer : IInitializer<IHttpManager>
{
    #region 属性变量
    /// <summary>
    /// 上下文插件
    /// </summary>
    [Inject(Required = true, Key = MIDDLEWARE_RunContext)]
    protected IHttpMiddleware RunContextMiddleware { init; get; } = null!;
    /// <summary>
    /// 遥测追踪插件
    /// </summary>
    [Inject(Required = true, Key = MIDDLEWARE_Telemetry)]
    protected IHttpMiddleware TelemetryMiddleware { init; get; } = null!;
    #endregion

    #region  IInitializer<IHttpManager>
    /// <summary>
    /// 初始化处理
    /// </summary>
    /// <param name="manager"></param>
    void IInitializer<IHttpManager>.Initialize(IHttpManager manager)
    {
        manager.Use(MIDDLEWARE_RunContext, RunContextMiddleware)
               .Use(MIDDLEWARE_Telemetry, TelemetryMiddleware);
    }
    #endregion
}