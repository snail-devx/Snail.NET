using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Message;
using Snail.Abstractions.Message.Extensions;
using Snail.Abstractions.Message.Interfaces;

namespace Snail.Message.Components;

/// <summary>
/// 消息初始化器；用于进行<see cref="IMessageManager"/>实例初始化
/// </summary>
[Component<IInitializer<IMessageManager>>(Lifetime = LifetimeType.Transient)]
public class MessageInitializer : IInitializer<IMessageManager>
{
    #region 属性变量
    /// <summary>
    /// 上下文插件
    /// </summary>
    [Inject(Required = true, Key = MIDDLEWARE_RunContext)]
    protected IMessageMiddleware RunContextMiddleware { init; get; } = null!;
    /// <summary>
    /// 遥测追踪插件
    /// </summary>
    [Inject(Required = true, Key = MIDDLEWARE_Telemetry)]
    protected IMessageMiddleware TelemetryMiddleware { init; get; } = null!;
    #endregion

    #region IInitializer<IMessageManager>
    /// <summary>
    /// 初始化消息
    /// </summary>
    /// <param name="manager">消息管理器</param>
    void IInitializer<IMessageManager>.Initialize(IMessageManager manager)
    {
        //  配置日志和运行上下文中间件
        manager.Use(MIDDLEWARE_RunContext, RunContextMiddleware)
               .Use(MIDDLEWARE_Telemetry, TelemetryMiddleware);
    }
    #endregion
}