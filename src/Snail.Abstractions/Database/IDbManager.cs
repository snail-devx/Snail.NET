using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Abstractions.Database;

/// <summary>
/// 接口约束：数据库管理器
/// <para>1、管理数据库服务器地址 </para>
/// <para>2、规划数据中间件逻辑，支持做干涉，如api拦截基于主键id的数据请求，然后走缓存 </para>
/// </summary>
public interface IDbManager
{
    /// <summary>
    /// 注册服务器
    /// <para>1、确保“Workspace+DbType+DbCode”唯一</para>
    /// <para>2、重复注册以最后一个为准</para>
    /// </summary>
    /// <param name="descriptors">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    IDbManager RegisterServer(params IList<DbServerDescriptor> descriptors);

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    /// <param name="options">服务器配置信息</param>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置</param>
    /// <param name="null2Error">为null时是否报错</param>
    /// <returns></returns>
    DbServerDescriptor? GetServer(IDbServerOptions options, bool isReadonly = false, bool null2Error = false);
    /// <summary>
    /// 尝试获取服务器信息
    /// <para>1、在仅知道<paramref name="workspace"/>和<paramref name="dbCode"/>时 </para>
    /// <para>2、取服务器信息中，第一个匹配<paramref name="workspace"/>和<paramref name="dbCode"/>的服务器信息 </para>
    /// </summary>
    /// <param name="workspace">服务器所属工作空间</param>
    /// <param name="dbCode">数据库编码</param>
    /// <param name="server">out参数：匹配到的服务器信息</param>
    /// <returns>匹配到了返回true；否则false</returns>
    bool TryGetServer(string? workspace, string dbCode, out DbServerDescriptor? server);
}
