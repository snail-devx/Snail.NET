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
    /// 应用程序
    /// </summary>
    protected readonly IApplication App;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public HttpInitializer(IApplication app)
    {
        App = ThrowIfNull(app);
    }
    #endregion

    #region  IInitializer<IHttpManager>
    /// <summary>
    /// 初始化处理
    /// </summary>
    /// <param name="manager"></param>
    /// <exception cref="NotImplementedException"></exception>
    void IInitializer<IHttpManager>.Initialize(IHttpManager manager)
    {
        manager.Use(MIDDLEWARE_RunContext, App.ResolveRequired<IHttpMiddleware>(key: MIDDLEWARE_RunContext))
               .Use(MIDDLEWARE_Telemetry, App.ResolveRequired<IHttpMiddleware>(key: MIDDLEWARE_Telemetry));
    }
    #endregion
}