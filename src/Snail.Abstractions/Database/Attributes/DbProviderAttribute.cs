using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Interfaces;
using Snail.Abstractions.Dependency.Interfaces;

namespace Snail.Abstractions.Database.Attributes;

/// <summary>
/// 特性标签：数据库提供程序实现类依赖注入<br />
///     1、使用<see cref="DbType"/>作为依赖注入的Key值<br />
///     2、生命周期<see cref="Lifetime"/>默认单例<br />
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DbProviderAttribute<Provider> : Attribute, IComponent
{
    #region 属性变量
    /// <summary>
    /// 数据库类型；将作为依赖注入的Key值
    /// </summary>
    public required DbType DbType { init; get; }
    #endregion

    #region IComponent
    /// <summary>
    /// 依赖注入Key值，用于DI动态构建实例 <br />
    ///     1、用于区分同一个源（From）多个实现（to）的情况 <br />
    /// </summary>
    /// <remarks>虽然和<see cref="IInject"/>中Key值意义一样，也不继承，避免接口串了</remarks>
    string? IComponent.Key => DbType.ToString();

    /// <summary>
    /// 源类型：当前组件实现哪个类型 <br />
    ///     1、为null时取自身作为From；不分析接口、基类等，如实现了IDisposable等系统接口，分析了占地方 <br />
    /// </summary>
    Type? IComponent.From => typeof(Provider);

    /// <summary>
    /// 组件生命周期，默认【瞬时】
    /// </summary>
    public LifetimeType Lifetime { get; init; } = LifetimeType.Singleton;
    #endregion
}

/// <summary>
/// 特性标签：数据库提供程序的注入配置 <br />
///     1、基于workspace、dbcode自动查找服务器地址信息，生成注入Key，<see cref="IInject.GetKey(IDIManager)"/> <br />
///     2、基于worksapce、dbcode自动生成注入参数<see cref="IDbServerOptions"/>，<see cref="IParameter{IDbServerOptions}.GetParameter(in IDIManager)"/><br />
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class DbProviderAttribute : Attribute, IInject, IParameter<IDbServerOptions>
{
    #region 属性变量
    /// <summary>
    /// 数据库服务器所属工作空间
    /// </summary>
    public string? Workspace { init; get; }
    /// <summary>
    /// 数据库编码
    /// </summary>
    public required string DbCode { init; get; }
    /// <summary>
    /// 数据库类型
    /// </summary>
    /// <remarks>除非存在<see cref="DbCode"/>相同的不同数据库类型配置，否则可忽略，会自动匹配值</remarks>
    public DbType? DbType { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    public DbProviderAttribute() { }
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="dbType"></param>
    public DbProviderAttribute(DbType dbType)
    {
        DbType = dbType;
    }
    #endregion

    #region IInject
    /// <summary>
    /// 依赖注入Key值，用于DI动态构建实例 <br />
    ///     1、用于区分同一个源（From）多个实现（to）的情况 <br />
    ///     2、默认值为null
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    string? IInject.GetKey(IDIManager manager)
    {
        IDbServerOptions server = Convert(manager);
        return server.DbType.ToString();
    }
    #endregion

    #region IParameter
    /// <summary>
    /// 获取参数值；由外部自己构建
    /// </summary>
    /// <param name="manager">DI管理器实例</param>
    /// <returns></returns>
    IDbServerOptions? IParameter<IDbServerOptions>.GetParameter(in IDIManager manager)
        => Convert(manager);
    #endregion

    #region 私有方法
    /// <summary>
    /// 转换成DbServerOptions对象
    /// </summary>
    /// <param name="manager"></param>
    /// <returns></returns>
    private IDbServerOptions Convert(IDIManager manager)
    {
        DbType dbType;
        if (DbType == null)
        {
            IDbManager dbManager = manager.ResolveRequired<IDbManager>();
            dbManager.TryGetServer(Workspace, DbCode, out DbServerDescriptor? descriptor);
            ThrowIfNull(descriptor, $"TryGetServer：获取数据库服务器信息失败。workspace:{Workspace};dbcode:{DbCode}");
            dbType = descriptor!.DbType;
        }
        else
        {
            dbType = DbType.Value;
        }

        return new DbServerOptions()
        {
            Workspace = Workspace,
            DbType = dbType,
            DbCode = DbCode,
        };
    }
    #endregion
}
