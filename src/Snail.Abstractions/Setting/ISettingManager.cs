using Snail.Abstractions.Setting.Delegates;

namespace Snail.Abstractions.Setting;

/// <summary>
/// 应用程序配置信息管理器
/// <para>1、管理环境变量；di、server等配置 </para>
/// <para>2、基于【工作空间-资源编码】、【工作空间-项目-资源编码】格式遍历资源清单，基于Application.config作为清单文件入口 </para>
/// </summary>
public interface ISettingManager
{
    /// <summary>
    /// 应用程序配置的工作目录
    /// </summary>
    string WorkDirectory { get; }

    /// <summary>
    /// 使用用应用程序的指定配置配置 
    /// <para>1、监听什么配置，有变化时谁处理 </para>
    /// <para>2、满足程序初始化读取配置，配置变化时通知外部变化 </para>
    /// </summary>
    /// <param name="isProject">false读取工作空间下配置；true读取工作空间-项目下的配置</param>
    /// <param name="rsCode">配置资源的编码</param>
    /// <param name="user">配置使用者</param>
    /// <returns>自身，链式调用</returns>
    ISettingManager Use(in bool isProject, in string rsCode, SettingUserDelegate user);

    /// <summary>
    /// 运行配置管理器
    /// </summary>
    void Run();

    /// <summary>
    /// 获取应用程序配置的环境变量值
    /// <para>备注：请在<see cref="Run"/>之后执行此方法 </para>
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <returns>配置值；若不存在返回null</returns>
    string? GetEnv(in string name);

    /* 先不提供get接口，使用use能够完全满足需求
    /// <summary>
    /// 获取配置内容
    /// </summary>
    /// <param name="workspace">工作空间编码</param>
    /// <param name="project">工作空间下项目编码；为空表示读取工作空间下配置，否则为指定项目下的配置</param>
    /// <param name="code">要读取的配置资源编码</param>
    /// <param name="type">配置资源内容类型；是文件、还是xml字符串、、、</param>
    /// <returns>配置内容，根据<paramref name="type"/>类型不一样，这里值不同
    /// <para>1、<see cref="SettingType.File"/>：为文件的绝对路径 </para>
    /// <para>2、<see cref="SettingType.Xml"/>：为xml内容字符串 </para>
    /// </returns>
    string? GetSetting(in string workspace, in string? project, in string code, out SettingType type);
     */
}
