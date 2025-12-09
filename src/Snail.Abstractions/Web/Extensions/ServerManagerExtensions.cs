using Snail.Abstractions.Web.DataModels;

namespace Snail.Abstractions.Web.Extensions;

/// <summary>
/// <see cref="IServerManager"/>扩展方法
/// </summary>
public static class ServerManagerExtensions
{
    #region GetServer
    /// <summary>
    /// 获取服务器信息；workspace、type为null
    /// </summary>
    /// <param name="manager">HTTP管理器实例</param>
    /// <param name="code">服务器编码</param>
    /// <returns></returns>
    public static ServerDescriptor? GetServer(this IServerManager manager, string code)
        => manager.GetServer(new ServerOptions(workspace: null, type: null, code));
    /// <summary>
    /// 获取服务器信息；type为null
    /// </summary>
    /// <param name="manager">HTTP管理器实例</param>
    /// <param name="workspace">服务器所在工作空间Key值</param>
    /// <param name="code">服务器编码</param>
    /// <returns></returns>
    public static ServerDescriptor? GetServer(this IServerManager manager, string workspace, string code)
        => manager.GetServer(new ServerOptions(workspace, type: null, code));
    /// <summary>
    /// 获取服务器信息
    /// </summary>
    /// <param name="manager">HTTP管理器实例</param>
    /// <param name="workspace">服务器所在工作空间Key值</param>
    /// <param name="type">服务器类型；用于对多个服务器做分组用</param>
    /// <param name="code">服务器编码</param>
    /// <returns></returns>
    public static ServerDescriptor? GetServer(this IServerManager manager, string workspace, string? type, string code)
        => manager.GetServer(new ServerOptions(workspace, type, code));
    #endregion
}
