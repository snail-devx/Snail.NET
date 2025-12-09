using Snail.Abstractions.Database;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Database.Components;

/// <summary>
/// <see cref="IDbProvider"/>基类
/// </summary>
public abstract class DbProvider : IDbProvider
{
    #region 属性变量
    /// <summary>
    /// 数据库管理器
    /// </summary>
    protected readonly IDbManager DbManager;
    /// <summary>
    /// 服务器配置选项
    /// </summary>
    protected readonly IDbServerOptions DbServer;
    #endregion

    #region 构造方法
    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    /// <param name="server"></param>
    public DbProvider(IApplication app, IDbServerOptions server)
    {
        ThrowIfNull(app);
        DbManager = app.ResolveRequired<IDbManager>();
        DbServer = ThrowIfNull(server);
    }
    #endregion

    #region IDbProvider
    /// <summary>
    /// 数据库服务器配置选项
    /// </summary>
    IDbServerOptions IDbProvider.DbServer => DbServer;
    #endregion
}
