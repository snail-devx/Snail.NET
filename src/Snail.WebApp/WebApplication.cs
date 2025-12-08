using Snail.Abstractions.Setting;
using Snail.Abstractions.Setting.Delegates;
using Snail.WebApp.Components;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp;

/// <summary>
/// 网络应用程序；负责webapi站点初始化等功能
/// </summary>
public class WebApplication : Application<IApplicationBuilder>, IApplication
{
    #region 事件、属性变量
    /// <summary>
    /// 事件：配置Web应用控制器 <br />
    ///     1、触发时机：<see cref="IApplication.Run"/> AddControllers添加控制器后 <br />
    ///     2、用途说明：对配置好的控制器增加功能，如自定义序列化和反序列化
    /// </summary>
    public event Action<IMvcBuilder>? OnController;

    /// <summary>
    /// WebApplication 对象构建器
    /// </summary>
    protected WebApplicationBuilder Builder { private init; get; }
    #endregion

    #region 构造方法
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="args"></param>
    /// <param name="initializer">app初始化器，传null则使用<see cref="WebAppInitializer"/>做初始化</param>
    public WebApplication(string[] args, IWebAppInitializer? initializer = null)
        : base()
    {
        /*base 父级会做如下初始化：di、setting实例初始化、强制内置DI服务注册*/

        //  1、webapi相关服务初始化
        Builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);
        //      替换内置ioc服务
        Builder.Host.UseServiceProviderFactory(new ServiceProviderFactory(DI));
        //  2、app自定义初始化工作；传null则使用默认初始化器
        (initializer ?? new WebAppInitializer()).InitApp(this);
    }
    #endregion

    #region IApplication
    /// <summary>
    /// 运行应用程序，执行顺序
    /// <para>1、内置服务注册（在app构造方法执行）</para>
    /// <para>2、扫描程序集，扫描<see cref="Type"/>完成特定<see cref="Attribute"/>分析注册，触发<see cref="IApplication.OnScan"/>事件</para>
    /// <para>3、读取应用程序配置，外部通过<see cref="ISettingManager.Use(in bool, in string, SettingUserDelegate)"/>使用配置</para>
    /// <para>4、自定义服务注册；触发<see cref="IApplication.OnRegister"/>事件，用于完成个性化di替换等</para>
    /// <para>5、控制器构建；触发<see cref="OnController"/> 进行API自定义配置</para>
    /// <para>6、应用构建；触发<see cref="Application{T}.OnBuild"/> 完成应用启动前自定义配置</para>
    /// <para>7、服务启动；触发<see cref="IApplication.OnRun"/>，运行WebApp应用</para>
    /// </summary>
    public void Run()
    {
        Microsoft.AspNetCore.Builder.WebApplication? app = null;
        //  1、执行基类run方法，完成程序集扫描、配置读取，OnScan、OnRegister、OnBuild、OnRun事件触发
        Run(() =>
        {
            //  添加控制器支持：AddControllers，支持Controller自定义，mvcbuilder干预；通过事件
            var mvc = Builder.Services.AddControllers();
            mvc.AddControllersAsServices();
            OnController?.Invoke(mvc);
            //  内部会固化IServiceCollection注册服务，并转换成ServiceProvider对外提供
            app = Builder.Build();
            return app;
        });
        //  2、映射控制器，启动服务响应api请求
        app!.MapControllers();
        app!.Run();
    }
    #endregion
}