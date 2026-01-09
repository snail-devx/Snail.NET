using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;
using Snail.WebApp.Components;

namespace Snail.WebApp;
/// <summary>
/// 网络应用程序：启动<see cref="IApplication.Run"/>时，执行顺序如下
/// <para>内部执行顺序：</para>
/// <para>1、内置服务注册（在app构造方法执行）</para>
/// <para>2、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="IApplication.OnScan"/>事件</para>
/// <para>3、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置</para>
/// <para>4、自定义服务注册；触发<see cref="IApplication.OnRegister"/>事件，用于完成个性化di替换等</para>
/// <para>5、自定义服务注册完成；触发<see cref="IApplication.OnRegistered"/>事件，用于进行一些服务、组件预热</para>
/// <para>6、Web应用程序构建：依次触发<see cref="OnBuild"/>、<see cref="OnController"/>、<see cref="OnBuilded"/>事件，进行控制器、网络中间件等配置</para>
/// <para>7、应用程序构建；触发<see cref="OnBuild"/>、<see cref="OnBuilded"/>事件，进行控制器、网络中间件等配置</para>
/// <para>8、服务启动；触发<see cref="IApplication.OnRun"/>，运行WebApp应用</para>
/// </summary>
public class WebApplication : Application, IApplication
{
    #region 事件、属性变量
    /// <summary>
    /// 事件：应用构建时
    /// <para>1、触发时机：<see cref="IApplication.OnRegistered"/> 之后</para>
    /// <para>2、用途说明：进行<see cref="WebApplicationBuilder"/> 进行自定义配置，如自定义Host宿主相关配置，添加后台任务等等 </para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="WebApplicationBuilder"/> 应用程序实例</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    public event Action<WebApplicationBuilder, IDIManager>? OnBuild;
    /// <summary>
    /// 事件：配置Web应用控制器
    /// <para>1、触发时机：<see cref="OnBuild"/>事件后，构建控制器是 </para>
    /// <para>2、用途说明：对配置好的控制器增加功能，如自定义序列化和反序列化 </para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="IMvcBuilder"/>mvc构建器实例</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// <para>4、备注说明：</para>
    /// <para>- 实际属于<see cref="OnBuild"/>事件内部逻辑，这里为了体现【控制器】概念和简化外部逻辑，单独抽取此事件</para>
    /// <para>- 触发此事件前会执行.AddControllers().AddControllersAsServices()等进行基础配置</para>
    /// </summary>
    public event Action<IMvcBuilder, IDIManager>? OnController;
    /// <summary>
    /// 事件：应用构建完成后
    /// <para>1、触发时机：<see cref="OnBuild"/> 之后</para>
    /// <para>2、用途说明：进行<see cref="Microsoft.AspNetCore.Builder.WebApplication"/>配置，如站点跨域，HTTP请求中间件 </para>
    /// <para>3、参数说明：</para>
    /// <para>- <see cref="Microsoft.AspNetCore.Builder.WebApplication"/> 应用程序实例</para>
    /// <para>- <see cref="IDIManager"/> 为根服务注入实例</para>
    /// </summary>
    public event Action<Microsoft.AspNetCore.Builder.WebApplication, IDIManager>? OnBuilded;

    /// <summary>
    /// 应用启动时的参数
    /// </summary>
    private readonly string[] _args;
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="args">程序启动时传入参数</param>
    /// <param name="initializer">初始化器，用于监听app的事件，进行程序默认初始化；为null则使用<see cref="WebAppInitializer"/></param>
    public WebApplication(string[] args, IInitializer<WebApplication>? initializer = null)
        : base()
    {
        _args = args;
        (initializer ?? new WebAppInitializer()).Initialize(this);
    }
    #endregion

    #region 重写父类方法
    /// <summary>
    /// 启动应用构建
    /// </summary>
    protected override void StartBuild()
    {
        //  1、基类应用基础构建
        base.StartBuild();
        //  2、初始化构建器：强制替换内置ioc服务；触发OnBuild、OnController事件
        WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(_args);
        builder.Host.UseServiceProviderFactory(new ServiceProviderFactory(RootServices));
        builder.Host.ConfigureHostOptions(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));
        //      触发 OnBuild 事件
        OnBuild?.Invoke(builder, RootServices);
        OnBuild = null;
        //      添加控制器支持：AddControllers，支持Controller自定义，mvcbuilder干预；
        IMvcBuilder mvc = builder.Services.AddControllers().AddControllersAsServices();
        OnController?.Invoke(mvc, RootServices);
        OnController = null;
        //  3、构建应用：内部会固化IServiceCollection注册服务，并转换成ServiceProvider对外提供
        Microsoft.AspNetCore.Builder.WebApplication app = builder.Build();
        //builder.Services.AddHostedService()
        OnBuilded?.Invoke(app, RootServices);
        OnBuilded = null;
        //      监听【OnRun】事件：启动WebApp；添加控制器映射，启动站点接收请求
        OnRun += (_) =>
        {
            app.MapControllers();
            app.Run();
        };
        //      监听关闭生命周期，执行Stop方法进行应用程序关闭处理：最大等待10s后强制关闭，避免尝试占用
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            Task? task = Stop();
            task?.Wait(TimeSpan.FromSeconds(10));
        });
    }
    #endregion
}