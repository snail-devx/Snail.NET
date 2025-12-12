using Snail.Abstractions.Common.Delegates;
using Snail.Abstractions.Setting;

namespace Snail.Abstractions;

/// <summary>
/// 接口约束：应用程序，约束框架下一个完整应用程序逻辑
/// <para>1、信息注入，如环境信息、上下文工作目录等</para>
/// <para>2、串联依赖注入、MQ消息等核心逻辑</para>
/// </summary>
public interface IApplication
{
    #region 事件、属性、方法约束
    /// <summary>
    /// 事件：应用扫描时
    /// <para>1、触发时机：<see cref="Run"/>时，首先执行程序扫描</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、只有打上<see cref="AppScanAttribute"/>标签的<see cref="Assembly"/>才会被扫描</para>
    /// <para>2、只有有<see cref="Attribute"/>标签的<see cref="Type"/>才会被扫描</para>
    /// </remarks>
    event AppScanDelegate? OnScan;
    /// <summary>
    /// 事件：服务注册时
    /// <para>1、触发时机：系统内置依赖注入注册完成后</para>
    /// <para>2、用于外部覆盖内置依赖注入配置完成个性化配置</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、不要在此事件中进行对象构建，否则可能导致依赖注入关系错误</para>
    /// </remarks>
    event Action? OnRegister;
    /// <summary>
    /// 事件：服务注册完成时
    /// <para>1、触发实际：事件<see cref="OnRegister"/>之后执行</para>
    /// <para>2、用于进行一些服务预热处理，如提前构建实例</para>
    /// </summary>
    event Action? OnRegistered;
    /// <summary>
    /// 事件：程序运行时触发
    /// <para>1、触发时机：app配置完成，准备启动前触发</para>
    /// <para>2、用于启动依赖的相关服务，如启动mq接收消息</para>
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
    /// 运行应用程序，执行顺序
    /// <para>1、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="OnScan"/>事件</para>
    /// <para>2、读取应用程序配置，外部通过<see cref="ISettingManager.Use"/>使用配置 </para>
    /// <para>3、自定义服务注册；触发<see cref="OnRegister"/>事件，用于完成个性化di替换等</para>
    /// <para>4、自定义服务注册完成；触发<see cref="OnRegistered"/>事件，用于进行一些服务、组件预热</para>
    /// <para>5、服务启动；触发<see cref="OnRun"/>事件</para>
    /// </summary>
    void Run();
    #endregion
}
