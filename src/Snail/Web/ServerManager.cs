using System.Xml;
using Snail.Abstractions.Setting.Enumerations;
using Snail.Abstractions.Web;
using Snail.Abstractions.Web.DataModels;
using Snail.Abstractions.Web.Interfaces;
using Snail.Utilities.Collections;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;

namespace Snail.Web
{
    /// <summary>
    /// 服务器管理器 <br />
    ///     1、注册管理服务器配置 <br />
    ///     2、自动读取应用程序指定资源配置作为初始服务器配置 <br />
    /// </summary>
    /// <remarks>可继承此类做扩展，完成自定义功能，如Http、Redis、Rabbitmq等服务器地址管理</remarks>
    [Component<IServerManager>(Lifetime = LifetimeType.Singleton)]
    public class ServerManager : IServerManager
    {
        #region 属性变量
        /// <summary>
        /// 服务器地址信息
        /// </summary>
        private readonly LockList<ServerDescriptor> _servers = new();
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="app">应用程序实例</param>
        /// <param name="rsCode">服务器配置资源编码，非空时自动读取</param>
        public ServerManager(IApplication app, string? rsCode)
        {
            ThrowIfNull(app);
            //  使用指定配置初始化服务器地址默认配置
            if (rsCode?.Length > 0)
            {
                app.UseSetting(isProject: false, rsCode: rsCode, WatchServerSetting);
            }
        }
        #endregion

        #region IServerManager
        /// <summary>
        /// 注册服务器：确保“Workspace+Type+Code”唯一，重复注册以第一个为准
        /// </summary>
        /// <param name="servers">服务器信息</param>
        /// <returns>管理器自身，方便链式调用</returns>
        IServerManager IServerManager.RegisterServer(IList<ServerDescriptor> servers)
        {
            ThrowIfHasNull(servers!, $"{nameof(servers)}存在为null的数据");
            _servers.AddRange(servers);
            return this;
        }

        /// <summary>
        /// 获取服务器信息
        /// </summary>
        /// <param name="options">服务器配置信息</param>
        /// <returns></returns>
        ServerDescriptor? IServerManager.GetServer(IServerOptions options)
        {
            ThrowIfNull(options);
            ServerDescriptor? descriptor = _servers.Get(
                predicate: server => server.Workspace == options.Workspace && server.Type == options.Type && server.Code == options.Code,
                isDescending: false
            );
            return descriptor;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 监听【服务器】配置变动
        /// </summary>
        /// <param name="workspace">配置所属工作空间</param>
        /// <param name="project">配置所属项目；为null表示工作空间下的资源，如服务器地址配置等</param>
        /// <param name="rsCode">配置资源的编码，唯一</param>
        /// <param name="type">配置类型，配置文件，后续支持</param>
        /// <param name="content">配置内容，根据<paramref name="type"/>类型不一样，这里值不同<br />
        ///     1、<see cref="SettingType.File"/>：<paramref name="content"/>为文件的绝对路径<br />
        ///     2、<see cref="SettingType.Xml"/>：<paramref name="content"/>为xml内容字符串
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
                    string server = Default(add.GetAttribute("server"), defaultStr: null)
                        ?? throw new ApplicationException($"服务器add节点server属性为空。{exPrefix}[code={code}]");
                    descriptors.Add(new ServerDescriptor(workspace, serverType, code: code, server));
                }
            }
            //  执行接口方法做注册，方便做唯一区分验证
            (this as IServerManager).RegisterServer(descriptors);
        }
        #endregion
    }
}
