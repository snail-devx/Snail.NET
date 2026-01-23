using Snail.Abstractions.Common.Interfaces;
using Snail.Abstractions.Setting.Extensions;
using Snail.Utilities.Collections.Extensions;
using Snail.Utilities.Common.Extensions;
using Snail.WebApp.Extensions;

namespace Snail.WebApp.Components;

/// <summary>
/// Web应用程序初始化器；只能用于初始化<see cref="WebApplication"/>
/// </summary>
public class WebAppInitializer : IInitializer<WebApplication>
{
    #region IInitializer<WebApplication>
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="application"></param>
    void IInitializer<WebApplication>.Initialize(WebApplication application)
    {
        //  监听【OnController】事件，完成控制器默认配置
        application.OnController += (builder, services) =>
        {
            //  控制器自定义配置选项
            builder.AddMvcOptions(options =>
            {
                //  允许空输入实体绑定；否则post时若不提交内容，会报错 A non-empty request body is required
                /*      https://github.com/aspnet/Mvc/issues/6920*/
                options.AllowEmptyInputInBodyModelBinding = true;
                //  是否禁用null类型的隐式 Required 特性的推断：具体作用，参照 AllowRequiredNullInference 属性
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = false;
            });
            /** 添加JSON序列化支撑：下面这些代码记录，先默认不放开；让系统采用默认行为
            //      默认会把属性进行驼峰命名；
            builder.AddJsonOptions(options =>
            {
                // 关闭属性名转换，保持原样（PascalCase）
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                // 如果有字典键也想保持原样，同样关闭
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            });
            //      添加NewtonsoftJson支持后，默认会驼峰命名；可以通过如何配置重置
            builder.AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            */
            //  添加NewtonsoftJson支持，扩展支持自定义类型推断
            {
                JsonBootstrapper bootstrapper = (JsonBootstrapper?)(services.Resolve<IEnumerable<IBootstrapper>>()?.FirstOrDefault(item => item is JsonBootstrapper))
                    ?? new JsonBootstrapper(application);
                builder.AddNewtonsoftJson(options => bootstrapper.UseCustomJsonConverter(options.SerializerSettings));
            }
        };
        //  监听OnBuild事件，完成web应用内置中间件集成
        application.OnBuilded += (app, services) =>
        {
            //  配置端口监听：从环境变量 Urls 中获取；如 http://*:4000
            application.GetEnv("Urls")
                ?.Split(";", StringSplitOptions.RemoveEmptyEntries)
                ?.ForEach(app.Urls.Add);
            //  中间配置
            app.UseRereadRequestBody()/*                            重复读取：解决actionfilter取不到request.Body数据的问题*/
               .UseUrlCors()/*                                      添加CORS支持*/
               .UseMiddleware<CookieMiddleware>()/*                 Cookie插件：解决cookie值包含“{”、“}”等关键字时，无法识别的问题*/
               .UseMiddleware<RunContextMiddleware>()/*             运行时上下文：为每个请求构建全新的运行时上下文，互不干扰*/
               .UseMiddleware<TelemetryMiddleware>();/*             遥测中间件：整理遥测信息数据*/
        };
    }
    #endregion
}
