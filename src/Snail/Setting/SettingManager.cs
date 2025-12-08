using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;
using Snail.Abstractions.Setting.Enumerations;
using Snail.Setting.DataModels;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.Utilities.IO.Utils;
using Snail.Utilities.Xml.Extensions;
using Snail.Utilities.Xml.Utils;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Snail.Setting
{
    /// <summary>
    /// 应用程序配置管理器：基于文件完成配置管理
    /// </summary>
    public sealed class SettingManager : ISettingManager
    {
        #region 属性变量
        /// <summary>
        /// 配置文件的根目录
        /// </summary>
        private readonly string _root;
        /// <summary>
        /// 应用程序配置 使用者
        /// </summary>
        private readonly List<SettingUser> _users = new List<SettingUser>();

        /// <summary>
        /// 环境变量配置信息
        /// </summary>
        private readonly Dictionary<string, string> _envs = new Dictionary<string, string>();
        /// <summary>
        /// 应用程序配置资源
        /// </summary>
        private readonly List<FileResourceDescriptor> _resources = new List<FileResourceDescriptor>();

        /// <summary>
        /// 管理器是否已经run过了
        /// </summary>
        private bool _hasRan = false;
        #endregion

        #region 构造方法
        /// <summary>
        /// 默认无参构造方法
        /// </summary>
        public SettingManager()
        {
            _root = Path.Combine(AppContext.BaseDirectory, "App_Setting");
        }
        #endregion

        #region ISettingManager
        /// <summary>
        /// 应用程序配置的工作目录
        /// </summary>
        string ISettingManager.WorkDirectory => _root;

        /// <summary>
        /// 使用用应用程序的指定配置配置 <br />
        ///     1、监听什么配置，有变化时谁处理<br />
        ///     2、满足程序初始化读取配置，配置变化时通知外部变化<br />
        /// </summary>
        /// <param name="isProject">false读取工作空间下配置；true读取工作空间-项目下的配置</param>
        /// <param name="rsCode">配置资源的编码</param>
        /// <param name="user">配置使用者</param>
        /// <returns>自身，链式调用</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        ISettingManager ISettingManager.Use(in bool isProject, in string rsCode, SettingUserDelegate user)
        {
            //  后期考虑把user直接注册到SettingResourceDescriptor中管理；这样后续使用时会比较方便，但没有资源的use，后期动态加上资源时会比较麻烦
            ThrowIfNullOrEmpty(rsCode);
            ThrowIfNull(user);
            SettingUser su = new() { IsProject = isProject, Code = rsCode, User = user };
            _users.Add(su);
            //  如果配置已经读取过了，则这里需要单独针对此使用者，立马做一下运行操作
            if (_hasRan == true)
            {
                RunUsers(_resources, [su]);
            }
            return this;
        }

        /// <summary>
        /// 运行配置管理器
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        void ISettingManager.Run()
        {
            //  读取配置  Application.config；若根目录不存在，则忽略读取
            if (Directory.Exists(_root) == true)
            {
                XmlDocument application = XmlHelper.Load(Path.Combine(_root, "Application.config"));
                BuildEnvironment(application);
                BuildResources(application);
            }
            //  运行配置，通知使用者读取配置
            RunUsers(_resources, _users);
            _hasRan = true;
        }

        /// <summary>
        /// 获取应用程序配置的环境变量值 <br />
        /// 备注：请在<see cref="ISettingManager.Run"/>之后执行此方法
        /// </summary>
        /// <param name="name">环境变量名称</param>
        /// <returns>配置值；若不存在返回null</returns>
        string? ISettingManager.GetEnv(in string name)
        {
            ThrowIfNull(name);
            _envs.TryGetValue(name, out string? value);
            return value;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 构建环境变量配置
        /// </summary>
        /// <param name="doc"></param>
        private void BuildEnvironment(in XmlDocument doc)
        {
            //  1、Application.config集成模式；配置了 envs节点   <add key="name" value="站点名称：记日志等使用" />
            IDictionary<string, string>? inEnvs = doc.SelectNodes("/configuration/envs/add")
                                                   ?.ToDictionary("环境变量");
            //  2、Application.config文件模式；配置了 <environment file="Environment.config" />
            IDictionary<string, string>? fileEnvs = null;
            XmlNode? fileNode = doc.SelectSingleNode("/configuration/environment");
            if (fileNode != null)
            {
                string file = fileNode.GetAttribute("file");
                if (string.IsNullOrEmpty(file) == true)
                {
                    string msg = "Application.config下“/configuration/environment”节点的file属性值为空；无法解析环境变量值";
                    throw new ApplicationException(msg);
                }
                file = Path.Combine(_root, file);
                //  暂时先支持默认section，后期查找指定section 名称节点数据
                fileEnvs = XmlHelper.Load(file)
                                    .SelectNodes("/configuration/section/add")
                                    ?.ToDictionary($"环境变量:{file}");
            }
            //  3、合并环境变量；优先File模式
            _envs.Combine(inEnvs!, fileEnvs!);
        }
        /// <summary>
        /// 构建配置资源
        /// </summary>
        /// <param name="doc"></param>
        private void BuildResources(in XmlDocument doc)
        {
            XmlNodeList? workspaces = doc.SelectNodes("/configuration/workspaces/workspace");
            if (workspaces == null || workspaces.Count == 0) return;
            //  遍历读取工作空间：解析工作空间编码，确保唯一性
            foreach (XmlNode workNode in workspaces)
            {
                string workspace = workNode.GetAttribute("code");
                if (string.IsNullOrEmpty(workspace))
                {
                    string msg = $"workspace节点code属性值无效，无法分析工作空间配置：${workNode.OuterXml}";
                    throw new ApplicationException(msg);
                }
                if (_resources.Any(rs => rs.Workspace == workspace) == true)
                {
                    string msg = $"已存在相同编码的工作空间，请检查code值：{workspace}";
                    throw new ApplicationException(msg);
                }
                //  构建直属空间资源
                XmlNode? resourceNode = workNode.SelectSingleNode("resource");
                _resources.TryAddRange(BuildByNode(workspace, project: null, resourceNode));
                //  解析下属项目的资源配置
                XmlNodeList? projects = workNode.SelectNodes("projects/project");
                if (projects == null || projects.Count == 0) continue;
                //      遍历读取项目下的资源配置；验证工作空间+项目code是否存在了，确保唯一性
                foreach (XmlNode projectNode in projects)
                {
                    string project = projectNode.GetAttribute("code");
                    if (string.IsNullOrEmpty(project))
                    {
                        string msg = $"project节点节点code属性值无效，无法分析工作空间[{workspace}]下的项目配置：${workNode.OuterXml}";
                        throw new ApplicationException(msg);
                    }
                    if (_resources.Any(rs => rs.Workspace == workspace && rs.Project == project) == true)
                    {
                        string msg = $"工作空间[{workspace}]下已存在相同编码的项目，请检查code值：{project}";
                        throw new ApplicationException(msg);
                    }
                    //  构建项目节点资源配置
                    _resources.TryAddRange(BuildByNode(workspace, project, projectNode));
                }
            }
        }
        /// <summary>
        /// 基于xml节点构建文件资源描述器
        /// </summary>
        /// <param name="workspace">工作空间</param>
        /// <param name="project">项目编码，为null表示是工作空间直属资源</param>
        /// <param name="node">资源节点</param>
        /// <returns></returns>
        private IList<FileResourceDescriptor>? BuildByNode(in string workspace, in string? project, in XmlNode? node)
        {
            //  强制忽略 code 节点，这个在项目下会作为特殊属性排除掉
            IDictionary<string, string>? attrs = node?.Attributes?.ToDictionary();
            if (attrs?.Any() == true)
            {
                attrs.Remove("code");
                //  组装资源文件地址，验证地址存在性
                string? dir = project == null ? Path.Combine(_root, workspace) : Path.Combine(_root, workspace, project);
                List<FileResourceDescriptor> resources = new List<FileResourceDescriptor>();
                foreach (var (code, value) in attrs)
                {
                    string path = Path.Combine(dir, value);
                    FileHelper.ThrowIfNotFound(path, $"配置文件不存在。Workspace:{workspace},project:{project},code:{code};file:{path}");
                    new FileResourceDescriptor() { Workspace = workspace, Project = project, Code = code, Path = path }
                        .AddTo(resources);
                }
                return resources;
            }
            return null;
        }

        /// <summary>
        /// 执行use代理方法
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="users">配置使用者</param>
        /// <remarks>先不将参数设置为in，否则在作为<see cref="RunIf{T1, T2}( bool,  Action{T1, T2},  T1,  T2)"/>参数时会提示不能将方法组转成委托</remarks>
        private static void RunUsers(IReadOnlyList<FileResourceDescriptor> resources, IReadOnlyList<SettingUser> users)
        {
            if (resources?.Any() != true || users?.Any() != true)
            {
                return;
            }
            // 遍历看此资源是否有监听器；后期这里考虑做多线程并发，如每个资源串行，每个资源的user并行；或者考虑把user直接注册到SettingResourceDescriptor中管理
            foreach (var res in resources!)
            {
                foreach (var user in users)
                {
                    //  资源编码一致；项目下资源
                    if (res.Code == user.Code && user.IsProject && res.Project?.Length > 0)
                    {
                        user.User(res.Workspace, res.Project, res.Code, SettingType.File, res.Path);
                        continue;
                    }
                    //  资源编码一致；工作空间直属资源
                    if (res.Code == user.Code && user.IsProject == false && res.Project == null)
                    {
                        user.User(res.Workspace, res.Project, res.Code, SettingType.File, res.Path);
                        continue;
                    }
                }
            }
        }
        #endregion
    }
}
