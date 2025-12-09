using Snail.Abstractions.Setting.Delegates;

namespace Snail.Setting.DataModels;

/// <summary>
/// 应用程序配置观察者
/// </summary>
internal sealed class SettingUser
{
    /// <summary>
    /// 是项目下资源吗？为false则资源为直属【工作区】的资源
    /// </summary>
    public bool IsProject { init; get; }

    /// <summary>
    /// 观察的资源编码
    /// </summary>
    public required string Code { init; get; }

    /// <summary>
    /// 观察者委托
    /// </summary>
    public required SettingUserDelegate User { init; get; }
}
