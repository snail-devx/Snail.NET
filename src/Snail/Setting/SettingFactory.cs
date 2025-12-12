using Snail.Abstractions.Setting;
using System.Runtime.CompilerServices;

namespace Snail.Setting;

/// <summary>
/// 应用程序配置工厂
/// </summary>
public static class SettingFactory
{
    #region 属性变量
    /// <summary>
    /// 管理器 构建器委托
    /// </summary>
    private static Func<ISettingManager>? _managerCreator;
    #endregion

    #region 公共方法
    /// <summary>
    /// 配置 构建器
    /// </summary>
    /// <param name="creator"></param>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void Config(in Func<ISettingManager> creator)
    {
        ThrowIfNull(creator);
        _managerCreator = creator;
    }

    /// <summary>
    /// 创建一个配置管理器
    /// <para>1、若为进行<see cref="Config"/>配置，则使用默认的配置管理器<see cref="SettingManager"/></para>
    /// </summary>
    /// <returns></returns>
    public static ISettingManager Create() => _managerCreator?.Invoke() ?? new SettingManager();
    #endregion
}
