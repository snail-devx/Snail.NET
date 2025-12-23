using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;

namespace Snail.Abstractions.Web;

/// <summary>
/// 接口约束：服务器管理器
/// <para>1、支撑http、rabbitmq、redis等可抽取成workspace-section-code模式管理服务器地址的模块 </para>
/// <para>2、配合各个管理器使用，实现实例不基于【依赖注入】做处理 </para>
/// </summary>
public interface IServerManager
{
    /// <summary>
    /// 注册服务器
    /// <para>1、确保“Workspace+Type+Code”唯一</para>
    /// <para>2、重复注册以最后一个为准</para>
    /// </summary>
    /// <param name="servers">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    IServerManager RegisterServer(params IList<ServerDescriptor> servers);

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    /// <param name="options">服务器配置信息</param>
    /// <returns></returns>
    ServerDescriptor? GetServer(IServerOptions options);
}
