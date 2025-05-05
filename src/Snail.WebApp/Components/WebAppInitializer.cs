using Snail.WebApp.Extensions;
using Snail.WebApp.Interfaces;

namespace Snail.WebApp.Components
{
    /// <summary>
    /// Web应用程序初始化器；只能用于初始化<see cref="WebApplication"/>
    /// </summary>
    public class WebAppInitializer : IWebAppInitializer
    {
        #region IAppInitializer
        /// <summary>
        /// 初始化App
        /// </summary>
        /// <param name="application"></param>
        public virtual void InitApp(WebApplication application)
        {
            //  监听【OnController】事件，完成控制器默认配置
            application.OnController += builder =>
            {
                //  依赖注入 自定义过滤器 
                builder.Services
                    .AddSingleton<ActionContentFilter>();
                //  控制器自定义配置选项
                builder.AddMvcOptions(options =>
                {
                    //  添加内置过滤器
                    options.Filters.Add(typeof(ActionContentFilter));
                    //  允许空输入实体绑定；否则post时若不提交内容，会报错 A non-empty request body is required
                    /*      https://github.com/aspnet/Mvc/issues/6920*/
                    options.AllowEmptyInputInBodyModelBinding = true;
                    //  是否禁用null类型的隐式 Required 特性的推断：具体作用，参照 AllowRequiredNullInference 属性
                    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = false;
                });
                //  添加JSON序列化支撑
                builder.AddNewtonsoftJson();
                //  将控制器作为服务
                builder.AddControllersAsServices();
            };
            //  监听OnBuild事件，完成web应用内置中间件集成
            application.OnBuild += app =>
            {
                app.UseUrlCors()
                   .UseCookieAdapter()
                   .UseRunContext()
                   .UseRereadRequestBody();
            };
        }
        #endregion
    }
}
