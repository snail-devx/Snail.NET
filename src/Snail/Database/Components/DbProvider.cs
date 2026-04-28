using Snail.Abstractions.Database;
using Snail.Abstractions.Database.Interfaces;

namespace Snail.Database.Components;
/// <summary>
/// 数据库提供程序基类
/// <para>1、存储数据库服务器配置等基础信息；纯粹为了复用代码</para>
/// <para>2、不继承<see cref="IDbProvider"/>，本身不做实现，继承意义不大</para>
/// </summary>
public abstract class DbProvider
{
    #region 属性变量
    /// <summary>
    /// 数据库管理器
    /// </summary>
    [Inject(Required = true)]
    protected IDbManager DbManager { init; get; } = null!;

    /// <summary>
    /// 服务器配置选项
    /// </summary>
    protected readonly IDbServerOptions DbServer;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="server"></param>
    public DbProvider(IDbServerOptions server)
    {
        DbServer = ThrowIfNull(server);
    }
    #endregion
}