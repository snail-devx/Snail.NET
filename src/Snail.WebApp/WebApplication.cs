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
    /// 运行应用程序，执行顺序 <br />
    ///     1、添加控制器：添加完控制器后，触发<see cref="OnController"/>事件，进行控制器选项自身配置 <br />
    ///     2、执行<see cref="WebApplicationBuilder.Build"/>构建web应用程序，固化内置服务 <br />
    ///     3、执行基类<see cref="Application{App}.Run(Func{App}?)"/>方法，完成程序集扫描、配置读取，OnScan、OnRegister、OnBuild、OnRun事件触发
    ///     4、执行<see cref="Microsoft.AspNetCore.Builder.WebApplication.Run(string?)"/>方法启动Web应用，响应api请求，
    /// </summary>
    public void Run()
    {
        //  1、构建web应用程序；先配置控制器支持，触发OnController事件、确保在应用程序的依赖注入信息之前，固化.NET自带服务注册
        Microsoft.AspNetCore.Builder.WebApplication app;
        {
            //  添加控制器支持：AddControllers，支持Controller自定义，mvcbuilder干预；通过事件
            var mvc = Builder.Services.AddControllers();
            mvc.AddControllersAsServices();
            OnController?.Invoke(mvc);
            //  内部会固化IServiceCollection注册服务，并转换成ServiceProvider对外提供
            app = Builder.Build();
        }
        //  2、启动程序
        //      执行基类run方法，完成程序集扫描、配置读取，OnScan、OnRegister、OnBuild、OnRun事件触发
        Run(() => app);
        //      映射控制器，启动服务响应api请求
        app.MapControllers();
        app.Run();
    }
    #endregion
}