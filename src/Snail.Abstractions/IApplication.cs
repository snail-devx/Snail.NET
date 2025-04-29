using Snail.Abstractions.Common.Delegates;
using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;

namespace Snail.Abstractions
{
    /// <summary>
    /// 接口约束：应用程序，约束框架下一个完整应用程序逻辑 <br />
    ///     1、信息注入，如环境信息、上下文工作目录等 <br />
    ///     2、串联依赖注入、MQ消息等核心逻辑
    /// </summary>
    public interface IApplication
    {
        #region 事件、属性、方法约束
        /// <summary>
        /// 事件：应用扫描时 <br />
        ///     1、触发时机：<see cref="Run"/>时，首先执行程序扫描 <br />
        /// 注意事项： <br />
        ///     1、只有打上<see cref="AppScanAttribute"/>标签的<see cref="Assembly"/>才会被扫描 <br />
        ///     2、只有有<see cref="Attribute"/>标签的<see cref="Type"/>才会被扫描 <br />
        /// </summary>
        event AppScanDelegate? OnScan;
        /// <summary>
        /// 事件：服务注册时 <br />
        ///     1、触发时机：系统内置依赖注入注册完成后 <br />
        ///     2、用于外部覆盖内置依赖注入配置完成个性化配置 <br />
        /// </summary>
        event Action? OnRegister;
        /// <summary>
        /// 事件：程序运行时触发 <br />
        ///     1、触发时机：app配置完成，准备启动前触发 <br />
        ///     2、用于启动依赖的相关服务，如启动mq接收消息
        /// </summary>
        event Action? OnRun;

        /// <summary>
        /// 应用依赖注入 管理器
        /// </summary>
        IDIManager DI { get; }
        /// <summary>
        /// 应用配置 管理器
        /// </summary>
        /// <remarks>暂时给内部使用，尽可能不对外暴露，通过app提供扩展方法，减少暴露面</remarks>
        internal ISettingManager Setting { get; }
        /// <summary>
        /// 应用根目录；默认exe所在目录，从<see cref="AppContext.BaseDirectory"/>取值
        /// </summary>
        string RootDirectory => AppContext.BaseDirectory;

        /// <summary>
        /// 运行应用程序，执行顺序 <br />
        ///     1、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="OnScan"/>事件<br />
        ///     2、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置 <br />
        ///     3、自定义服务注册；触发<see cref="OnRegister"/>事件，用于完成个性化di替换等<br />
        ///     4、服务启动；触发<see cref="OnRun"/>事件
        /// </summary>
        void Run();
        #endregion
    }
}
