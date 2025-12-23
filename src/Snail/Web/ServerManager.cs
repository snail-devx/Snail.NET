using Snail.Abstractions.Setting.Enumerations;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Collections;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;
using System.Xml;

namespace Snail.Web;

/// <summary>
/// 服务器管理器 
/// <para>1、注册管理服务器配置 </para>
/// <para>2、自动读取应用程序指定资源配置作为初始服务器配置 </para>
/// </summary>
/// <remarks>可继承此类做扩展，完成自定义功能，如Http、Redis、Rabbitmq等服务器地址管理</remarks>
[Component<IServerManager>]
public class ServerManager : IServerManager
{
    #region 属性变量
    /// <summary>
    /// 应用程序实例
    /// </summary>
    private readonly IApplication _app;
    /// <summary>
    /// 文件配置的服务器地址信息
    /// </summary>
    private readonly LockList<ServerDescriptor> _fileServers = new();
    /// <summary>
    /// 动态注册的服务器地址信息
    /// <para>1、调用<see cref="IServerManager.RegisterServer(IList{ServerDescriptor})"/>注册的服务器地址信息</para>
    /// <para>2、和<see cref="_fileServers"/>配置服务器地址信息区分开，避免文件配置自动变化时，冲掉动态注册的配置；从而影响程序逻辑</para>
    /// </summary>
    private readonly LockList<ServerDescriptor> _dynamicServers = new();
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="app">应用程序实例</param>
    /// <param name="rsCode">服务器配置资源编码，非空时自动读取</param>
    public ServerManager(IApplication app, string? rsCode)
    {
        _app = ThrowIfNull(app);
        //  使用指定配置初始化服务器地址默认配置
        if (rsCode?.Length > 0)
        {
            app.UseSetting(isProject: false, rsCode: rsCode, WatchServerSetting);
        }
    }
    #endregion

    #region IServerManager
    /// <summary>
    /// 注册服务器
    /// <para>1、确保“Workspace+Type+Code”唯一</para>
    /// <para>2、重复注册以最后一个为准</para>
    /// </summary>
    /// <param name="servers">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    IServerManager IServerManager.RegisterServer(IList<ServerDescriptor> servers)
    {
        RegisterServer(_dynamicServers, servers);
        return this;
    }

    /// <summary>
    /// 获取服务器信息
    /// </summary>
    /// <param name="options">服务器配置信息</param>
    /// <returns></returns>
    ServerDescriptor? IServerManager.GetServer(IServerOptions options)
    {
        //  优先从动态注册配置中读取；然后再读取文件配置
        ThrowIfNull(options);
        ServerDescriptor? descriptor = _dynamicServers.Get(predicate: server => PredicateServer(server, options), isDescending: false)
            ?? _fileServers.Get(predicate: server => PredicateServer(server, options), isDescending: false);
        return descriptor;
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 注册服务器配置
    /// </summary>
    /// <param name="target">目标服务器集合；将<paramref name="servers"/>注册到此列表中</param>
    /// <param name="servers">服务器信息</param>
    /// <returns>管理器自身，方便链式调用</returns>
    private static void RegisterServer(LockList<ServerDescriptor> target, IList<ServerDescriptor> servers)
    {
        ThrowIfNull(servers);
        ThrowIfHasNull(servers!, $"{nameof(servers)}存在为null的数据");
        foreach (var server in servers)
        {
            target.Replace(predict: item => PredicateServer(item, server), obj: server);
        }
    }
    /// <summary>
    /// 断言服务器配置信息
    /// </summary>
    /// <param name="server"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    private static bool PredicateServer(IServerOptions server, IServerOptions options)
        => server.Workspace == options.Workspace && server.Type == options.Type && server.Code == options.Code;

    /// <summary>
    /// 监听【服务器】配置变动
    /// </summary>
    /// <param name="workspace">配置所属工作空间</param>
    /// <param name="project">配置所属项目；为null表示工作空间下的资源，如服务器地址配置等</param>
    /// <param name="rsCode">配置资源的编码，唯一</param>
    /// <param name="type">配置类型，配置文件，后续支持</param>
    /// <param name="content">配置内容，根据<paramref name="type"/>类型不一样，这里值不同
    /// <para>1、<see cref="SettingType.File"/>：<paramref name="content"/>为文件的绝对路径 </para>
    /// <para>2、<see cref="SettingType.Xml"/>：<paramref name="content"/>为xml内容字符串 </para>
    /// </param>
    private void WatchServerSetting(string workspace, string? project, string rsCode, SettingType type, string content)
    {
        //  先仅支持【文件】类型配置读取
        if (type != SettingType.File)
        {
            string msg = $"{nameof(ServerManager)}读取配置时，仅支持File类型，当前为：{type.ToString()}；content:{content}";
            throw new ApplicationException(msg);
        }
        //  转换成xml做解析；servers
        XmlDocument doc = XmlHelper.Load(content);
        XmlNodeList? servers = doc.SelectNodes("/configuration/servers");
        if (servers == null || servers.Count == 0)
        {
            return;
        }
        List<ServerDescriptor> descriptors = new List<ServerDescriptor>();
        foreach (XmlNode node in servers)
        {
            string? serverType = Default(node.GetAttribute("type"), defaultStr: null);
            //  遍历查找子节点数据
            XmlNodeList? adds = node.SelectNodes("add");
            if (adds == null || adds.Count == 0)
            {
                continue;
            }
            string exPrefix = $"workspace:{workspace};code:{rsCode};xpath:/configuration/servers[type={serverType ?? STR_Null}]/add";
            foreach (XmlNode add in adds)
            {
                string code = Default(add.GetAttribute("code"), defaultStr: null)
                    ?? throw new ApplicationException($"服务器add节点code属性为空。{exPrefix}");
                //  server支持参数化
                string server = Default(_app.AnalysisVars(add.GetAttribute("server")), defaultStr: null)
                    ?? throw new ApplicationException($"服务器add节点server属性为空。{exPrefix}[code={code}]");
                descriptors.Add(new ServerDescriptor(workspace, serverType, code: code, server));
            }
        }
        //  文件配置服务器信息注册
        RegisterServer(_fileServers, descriptors);
    }
    #endregion
}
