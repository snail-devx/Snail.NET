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
    /// <para>2、用途说明：用于实现组件自动扫描注册</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// <para>- <see cref="Type"/> 为当前正在扫描的类型</para>
    /// <para>- <see cref="Attribute"/> 当前扫描类型的特性标签</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、只有打上<see cref="AppScanAttribute"/>标签的<see cref="Assembly"/>才会被扫描</para>
    /// <para>2、只有有<see cref="Attribute"/>标签的<see cref="Type"/>才会被扫描</para>
    /// </remarks>
    event Action<IDIManager, Type, ReadOnlySpan<Attribute>>? OnScan;
    /// <summary>
    /// 事件：服务注册时
    /// <para>1、触发时机：系统内置依赖注入注册完成后</para>
    /// <para>2、用途说明：用于外部覆盖内置依赖注入配置完成个性化配置</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    /// <remarks>
    /// 注意事项：
    /// <para>1、不要在此事件中进行对象构建，否则可能导致依赖注入关系错误</para>
    /// </remarks>
    event Action<IDIManager>? OnRegister;
    /// <summary>
    /// 事件：服务注册完成时
    /// <para>1、触发时机：事件<see cref="OnRegister"/>之后执行</para>
    /// <para>2、用途说明：用于进行服务预热处理，如提前构建实例</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>ara>
    /// </summary>
    event Action<IDIManager>? OnRegistered;
    /// <summary>
    /// 事件：程序运行时
    /// <para>1、触发时机：app配置完成，准备启动前触发</para>
    /// <para>2、用途说明：用于启动依赖的相关服务，如启动mq接收消息</para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    event Action<IDIManager>? OnRun;
    /// <summary>
    /// 事件：程序停止时
    /// <para>1、触发时机：程序关闭时触发</para>
    /// <para>2、用途说明：实现程序关闭前的资源销毁等处理，若内部为异步任务，则返回Task，否则返回null</para>
    /// </summary>
    event Func<Task?>? OnStop;

    /// <summary>
    /// 应用根目录；默认exe所在目录，从<see cref="AppContext.BaseDirectory"/>取值
    /// </summary>
    string RootDirectory => AppContext.BaseDirectory;
    /// <summary>
    /// 应用配置 管理器
    /// </summary>
    /// <remarks>暂时给内部使用，尽可能不对外暴露，通过app提供扩展方法，减少暴露面</remarks>
    internal protected ISettingManager Setting { get; }
    /// <summary>
    /// 依赖注入根服务
    /// </summary>
    /// <remarks>暂时不对外开放，仅针对内部使用</remarks>
    internal protected IDIManager RootServices { get; }
    /// <summary>
    /// 当前作用域的依赖注入服务
    /// <para>1、实现作用域之间实例隔离；如一个ASP.NET Core的HTTP请求，就是一个全新的作用域</para>
    /// <para>2、若为null，则从app根服务构建新的服务实例</para>
    /// </summary>
    IDIManager ScopeServices { get; }

    /// <summary>
    /// 运行应用程序
    /// </summary>
    void Run();
    /// <summary>
    /// 停止应用程序
    /// </summary>
    /// <returns>任务对象；若内部存在异步处理，则返回Task，方便外部等待优雅退出</returns>
    Task? Stop();
    #endregion
}