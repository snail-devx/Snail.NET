using Snail.Abstractions.Database;
using Snail.Abstractions.Database.DataModels;
using Snail.Abstractions.Database.Enumerations;
using Snail.Abstractions.Database.Interfaces;
using Snail.Abstractions.Setting.Enumerations;
using Snail.Utilities.Collections;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;
using System.Xml;

namespace Snail.Database;

/// <summary>
/// 数据库管理器：维护服务器地址
/// </summary>
[Component<IDbManager>]
public sealed class DbManager : IDbManager
{
    #region 属性变量
    /// <summary>
    /// 应用程序实例
    /// </summary>
    private readonly IApplication _app;
    /// <summary>
    /// 文件配置的服务器地址信息
    /// </summary>
    private readonly LockList<DbServerDescriptor> _fileServers = new();
    /// <summary>
    /// 动态注册的服务器配置信息
    /// <para>1、调用<see cref="IDbManager.RegisterServer(IList{DbServerDescriptor})"/>注册的服务器地址信息</para>
    /// <para>2、和<see cref="_fileServers"/>配置服务器地址信息区分开，避免文件配置自动变化时，冲掉动态注册的配置；从而影响程序逻辑</para>
    /// </summary>
    private readonly LockList<DbServerDescriptor> _dynamicServers = new();
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    public DbManager(IApplication app)
    {
        _app = ThrowIfNull(app);
        app.UseSetting(isProject: false, rsCode: "database", WatchDbServerSetting);
    }
    #endregion

    #region IDbManager
    /// <summary>
    /// 注册服务器
    /// <para>1、确保“Workspace+DbType+DbCode”唯一</para>
    /// <para>2、重复注册以最后一个为准</para>
    /// </summary>
    /// <param name="descriptors">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    IDbManager IDbManager.RegisterServer(IList<DbServerDescriptor> descriptors)
    {
        RegisterServer(_dynamicServers, descriptors);
        return this;
    }

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    /// <param name="options">服务器配置信息</param>
    /// <param name="isReadonly">是否是获取只读数据库服务器配置</param>
    /// <param name="null2Error">为null时是否报错</param>
    /// <returns></returns>
    DbServerDescriptor? IDbManager.GetServer(IDbServerOptions options, bool isReadonly, bool null2Error)
    {
        //  优先从动态注册配置中读取；然后再读取文件配置；后期再支持 读写分离；前期先直接返回
        ThrowIfNull(options);
        DbServerDescriptor? descriptor = _dynamicServers.Get(server => PredicateDbServer(server, options), isDescending: false)
            ?? _fileServers.Get(server => PredicateDbServer(server, options), isDescending: false);
        if (null2Error == true && descriptor == null)
        {
            string msg = $"获取数据库服务器信息失败。isReadonly:{isReadonly},options:{options.AsJson()}";
            throw new ApplicationException(msg);
        }
        return descriptor;
    }
    /// <summary>
    /// 尝试获取服务器信息
    /// <para>1、在仅知道<paramref name="workspace"/>和<paramref name="dbCode"/>时 </para>
    /// <para>2、取服务器信息中，第一个匹配<paramref name="workspace"/>和<paramref name="dbCode"/>的服务器信息 </para>
    /// </summary>
    /// <param name="workspace">服务器所属工作空间</param>
    /// <param name="dbCode">数据库编码</param>
    /// <param name="server">out参数：匹配到的服务器信息</param>
    /// <returns>匹配到了返回true；否则false</returns>
    bool IDbManager.TryGetServer(string? workspace, string dbCode, out DbServerDescriptor? server)
    {
        //  优先从动态注册配置中读取；然后再读取文件配置；
        server = _dynamicServers.Get(server => server.Workspace == workspace && server.DbCode == dbCode, isDescending: false)
            ?? _fileServers.Get(server => server.Workspace == workspace && server.DbCode == dbCode, isDescending: false);
        return server != null;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 注册服务器配置
    /// </summary>
    /// <param name="target">目标服务器集合；将<paramref name="servers"/>注册到此列表中</param>
    /// <param name="servers">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    private static void RegisterServer(LockList<DbServerDescriptor> target, IList<DbServerDescriptor> servers)
    {
        ThrowIfNull(servers);
        ThrowIfHasNull(servers!, $"{nameof(servers)}存在为null的数据");
        foreach (var server in servers)
        {
            target.Replace(predict: item => PredicateDbServer(item, server), obj: server);
        }
    }
    /// <summary>
    /// 查找断言是否是指定的服务器
    /// </summary>
    /// <param name="server"></param>
    /// <param name="options">服务器配置选项</param>
    /// <returns></returns>
    private static bool PredicateDbServer(IDbServerOptions server, IDbServerOptions options)
        => server.Workspace == options.Workspace && server.DbCode == options.DbCode && server.DbType == options.DbType;

    /// <summary>
    /// 监听【数据库】配置变动
    /// </summary>
    /// <param name="workspace">配置所属工作空间</param>
    /// <param name="project">配置所属项目；为null表示工作空间下的资源，如服务器地址配置等</param>
    /// <param name="rsCode">配置资源的编码，唯一</param>
    /// <param name="type">配置类型，配置文件，后续支持</param>
    /// <param name="content">配置内容，根据<paramref name="type"/>类型不一样，这里值不同
    /// <para>1、<see cref="SettingType.File"/>：<paramref name="content"/>为文件的绝对路径 </para>
    /// <para>2、<see cref="SettingType.Xml"/>：<paramref name="content"/>为xml内容字符串 </para>
    /// </param>
    private void WatchDbServerSetting(string workspace, string? project, string rsCode, SettingType type, string content)
    {
        //  先仅支持【文件】类型配置读取
        if (type != SettingType.File)
        {
            string msg = $"{nameof(DbManager)}读取配置时，仅支持File类型，当前为：{type.ToString()}；content:{content}";
            throw new ApplicationException(msg);
        }
        //  转换成xml做解析；servers
        XmlDocument doc = XmlHelper.Load(content);
        XmlNodeList? servers = doc.SelectNodes("/configuration/servers/add");
        if (servers == null || servers.Count == 0)
        {
            return;
        }
        IList<DbServerDescriptor> descriptors = new List<DbServerDescriptor>();
        foreach (XmlNode add in servers)
        {
            string xpath = $"workspace:{workspace};xpath:/configuration/servers/add";
            //  解析数据；数据库类型，需要做枚举转换
            string dbCode = Default(add.GetAttribute("dbcode"), defaultStr: null)
                    ?? throw new ApplicationException($"数据库add节点dbcode属性为空。{xpath}");
            DbType dbType;
            {
                string typeStr = Default(add.GetAttribute("dbtype"), defaultStr: null)
                    ?? throw new ApplicationException($"数据库add节点dbtype属性为空。{xpath}[dbcode={dbCode}]");
                xpath = $"{xpath}[dbtype={typeStr}]";
                if (typeStr.IsEnum(out dbType) == false)
                {
                    typeStr = $"数据库servers节点type无效。{xpath}";
                    throw new ApplicationException(typeStr);
                }
            }
            //      dbName和connection支持参数化
            string dbName = Default(_app.AnalysisVars(add.GetAttribute("dbname")), defaultStr: null)
                ?? throw new ApplicationException($"数据库add节点dbname属性为空。{xpath}");
            string connection = Default(_app.AnalysisVars(add.GetAttribute("connection")), defaultStr: null)
                ?? throw new ApplicationException($"数据库add节点connection属性为空。{xpath}");

            //  构建服务器信息描述器
            descriptors.Add(new DbServerDescriptor()
            {
                Workspace = workspace,
                DbCode = dbCode,
                DbType = dbType,
                DbName = dbName,
                Connection = connection
            });
        }
        //  文件配置服务器信息注册
        RegisterServer(_fileServers, descriptors);
    }
    #endregion
}
