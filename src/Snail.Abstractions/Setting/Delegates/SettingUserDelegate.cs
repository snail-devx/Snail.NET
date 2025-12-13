using Snail.Abstractions.Setting.Enumerations;

namespace Snail.Abstractions.Setting.Delegates;

/// <summary>
/// 委托：扫描配置
/// </summary>
/// <param name="workspace">配置所属工作空间</param>
/// <param name="project">配置所属项目；为null表示工作空间下的资源，如服务器地址配置等</param>
/// <param name="rsCode">配置资源的编码，唯一</param>
/// <param name="type">配置类型，配置文件，后续支持</param>
/// <param name="content">配置内容，根据<paramref name="type"/>类型不一样，这里值不同
/// <para>1、<see cref="SettingType.File"/>：<paramref name="content"/>为文件的绝对路径 </para>
/// <para>2、<see cref="SettingType.Xml"/>：<paramref name="content"/>为xml内容字符串 </para>
/// </param>
public delegate void SettingUserDelegate(string workspace, string? project, string rsCode, SettingType type, string content);