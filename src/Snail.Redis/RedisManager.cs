using Snail.Abstractions;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Collections;
using Snail.Utilities.Common.Extensions;
using Snail.Web;
using StackExchange.Redis;

namespace Snail.Redis;

/// <summary>
/// Redis服务器管理器
/// </summary>
[Component]
public sealed class RedisManager : ServerManager
{
    #region 属性变量
    /// <summary>
    /// 【StackExchange.Redis】操作redis的ConnectionMultiplexer对象缓存；
    /// </summary>
    private readonly static LockMap<string, ConnectionMultiplexer> _multiplexers = new();
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法；强制使用 rsCode为“redis”的配置资源
    /// </summary>
    /// <param name="app">应用程序实例</param>
    public RedisManager(IApplication app) : base(app, rsCode: "redis")
    { }
    #endregion

    #region 公共方法
    /// <summary>
    /// 获取redis数据库
    /// </summary>
    /// <param name="server">服务器配置选项</param>
    /// <param name="dbIndex">数据库索引，从0开始</param>
    /// <returns></returns>
    public IDatabase GetDatabase(IServerOptions server, int dbIndex)
    {
        ThrowIfNull(server);
        ServerDescriptor? descriptor = (this as IServerManager).GetServer(server);
        ThrowIfNull(descriptor, $"获取redis服务器地址失败：{server.AsJson()}");
        var multiplexer = _multiplexers.GetOrAdd(descriptor!.Server, key => ConnectionMultiplexer.Connect(key));
        return multiplexer.GetDatabase(dbIndex);
    }
    #endregion
}
