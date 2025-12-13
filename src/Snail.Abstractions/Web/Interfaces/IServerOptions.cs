namespace Snail.Abstractions.Web.Interfaces;

/// <summary>
/// 接口约束：服务器配置选项
/// </summary>
public interface IServerOptions
{
    /// <summary>
    /// 服务器所在工作空间Key值
    /// <para>1、为null根据实际需要走默认值或者报错 </para>
    /// </summary>
    public string? Workspace { get; }

    /// <summary>
    /// 服务器类型；用于对多个服务器做分组用
    /// <para>1、无分组的服务器取null即可 </para>
    /// <para>2、如http请求服务器，可分为http、https、sdk等分组，做不同用途使用 </para>
    /// </summary>
    public string? Type { get; }

    /// <summary>
    /// 服务器编码
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// 服务器配置选项转换为字符串
    /// </summary>
    /// <returns></returns>
    string ToString();
}
